using System;
using System.Collections.Generic;
using BenVoxel.Interfaces;
using BenVoxel.Structs;
using System.Linq;

namespace BenVoxel.Models;

public class SegmentedBrickModel : IEditableBrickModel
{
	private const byte SegmentSizeBricks = 64,
		ShiftSegment = 7, // Bits 7-15
		ShiftBrick = 1,   // Bits 1-6
		MaskBrick = 0x3F; // 0-63
	private const uint SegmentBrickVolume = SegmentSizeBricks * SegmentSizeBricks * SegmentSizeBricks; // 262,144 ulongs (2 MB)
	/// <summary>
	/// Storage: Sparse dictionary mapping Segment ID -> Dense Brick Array
	/// </summary>
	private readonly Dictionary<uint, ulong[]> _segments = [];
	private ushort _minX = ushort.MaxValue, _maxX = 0,
		_minY = ushort.MaxValue, _maxY = 0,
		_minZ = ushort.MaxValue, _maxZ = 0;
	private bool _isEmpty = true;
	#region Public API
	/// <summary>
	/// Represents the bounding box of the model.
	/// </summary>
	public readonly record struct Bounds(
		ushort MinX, ushort MaxX,
		ushort MinY, ushort MaxY,
		ushort MinZ, ushort MaxZ)
	{
		public ushort SizeX => (ushort)(MaxX - MinX + 2);
		public ushort SizeY => (ushort)(MaxY - MinY + 2);
		public ushort SizeZ => (ushort)(MaxZ - MinZ + 2);
	}
	/// <summary>
	/// segmentId, brickIndex, payload
	/// </summary>
	public event Action<uint, int, ulong> OnBrickDirty;
	public event Action<uint> OnSegmentLoaded;
	public event Action<uint> OnSegmentUnloaded;
	/// <summary>
	/// Fired when the model bounds expand due to a voxel/brick being set outside the current bounds.
	/// Parameters: (oldBounds, newBounds). oldBounds is null if model was previously empty.
	/// </summary>
	public event Action<Bounds?, Bounds> OnBoundsChanged;
	/// <summary>
	/// Gets the current bounding box of the model, or null if the model is empty.
	/// </summary>
	public Bounds? CurrentBounds => _isEmpty ? null : new Bounds(_minX, _maxX, _minY, _maxY, _minZ, _maxZ);
	public SegmentedBrickModel(IBrickModel source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		foreach (VoxelBrick brick in (IEnumerable<VoxelBrick>)source)
			SetBrick(brick.X, brick.Y, brick.Z, brick.Payload);
	}
	/// <summary>
	/// Sets a brick payload at the given world coordinates.
	/// Allocates memory if needed and fires change events.
	/// </summary>
	public void SetBrick(ushort x, ushort y, ushort z, ulong payload)
	{
		// Snap to brick origin
		x &= 0xFFFE;
		y &= 0xFFFE;
		z &= 0xFFFE;
		uint segmentId = GetSegmentId(x, y, z);
		int brickIndex = GetBrickIndex(x, y, z);
		// 1. Allocation
		if (!_segments.TryGetValue(segmentId, out ulong[] bricks))
		{
			// If payload is empty and segment doesn't exist, do nothing
			if (payload == 0) return;
			bricks = new ulong[SegmentBrickVolume];
			_segments[segmentId] = bricks;
			// Notify listeners (e.g. GPU Bridge)
			OnSegmentLoaded?.Invoke(segmentId);
		}
		// 2. State Change
		ulong oldPayload = bricks[brickIndex];
		if (oldPayload == payload) return; // Optimization: skip if unchanged
		bricks[brickIndex] = payload;
		// 3. Bounds Update
		if (payload != 0)
			UpdateBounds(x, y, z);
		// 4. Notification
		OnBrickDirty?.Invoke(segmentId, brickIndex, payload);
	}
	/// <summary>
	/// Explicitly unloads a segment to free memory.
	/// </summary>
	/// <remarks>
	/// Bounds only ever expand during SetBrick.
	/// Call RecalculateBounds() after bulk deletions.
	/// </remarks>
	public void UnloadSegment(ushort sx, ushort sy, ushort sz)
	{
		uint segmentId = PackSegmentId(sx, sy, sz);
		if (_segments.Remove(segmentId))
			OnSegmentUnloaded?.Invoke(segmentId);
	}
	public void Clear()
	{
		foreach (uint segmentId in _segments.Keys.ToArray())
			OnSegmentUnloaded?.Invoke(segmentId);
		_segments.Clear();
		_minX = ushort.MaxValue; _maxX = 0;
		_minY = ushort.MaxValue; _maxY = 0;
		_minZ = ushort.MaxValue; _maxZ = 0;
		_isEmpty = true;
	}
	public void ClearSegment(ushort sx, ushort sy, ushort sz)
	{
		uint segmentId = PackSegmentId(sx, sy, sz);
		if (!_segments.TryGetValue(segmentId, out ulong[] bricks))
			return;
		for (int i = 0; i < bricks.Length; i++)
		{
			if (bricks[i] == 0) continue;
			bricks[i] = 0;
			OnBrickDirty?.Invoke(segmentId, i, 0);
		}
		RecalculateBounds();
	}
	/// <summary>
	/// Forces a recalculation of the bounding box.
	/// Useful after performing many deletions.
	/// </summary>
	public void RecalculateBounds()
	{
		_minX = ushort.MaxValue; _maxX = 0;
		_minY = ushort.MaxValue; _maxY = 0;
		_minZ = ushort.MaxValue; _maxZ = 0;
		_isEmpty = true;
		foreach (VoxelBrick brick in this)
			UpdateBounds(brick.X, brick.Y, brick.Z);
	}
	/// <summary>
	/// Applies externally-computed bounds (e.g., from GPU compute shader).
	/// Only shrinks bounds, never expands. Fires OnBoundsChanged if bounds changed.
	/// </summary>
	/// <param name="computedBounds">The bounds computed externally, or null if model is empty.</param>
	public void ApplyTrimmedBounds(Bounds? computedBounds)
	{
		Bounds? oldBounds = CurrentBounds;

		if (!computedBounds.HasValue)
		{
			// Model is empty
			if (!_isEmpty)
			{
				_minX = ushort.MaxValue; _maxX = 0;
				_minY = ushort.MaxValue; _maxY = 0;
				_minZ = ushort.MaxValue; _maxZ = 0;
				_isEmpty = true;
				OnBoundsChanged?.Invoke(oldBounds, new Bounds(0, 0, 0, 0, 0, 0));
			}
			return;
		}

		Bounds newBounds = computedBounds.Value;

		// Only allow shrinking, not expansion (expansion happens via SetBrick)
		bool changed = false;
		if (newBounds.MinX > _minX) { _minX = newBounds.MinX; changed = true; }
		if (newBounds.MaxX < _maxX) { _maxX = newBounds.MaxX; changed = true; }
		if (newBounds.MinY > _minY) { _minY = newBounds.MinY; changed = true; }
		if (newBounds.MaxY < _maxY) { _maxY = newBounds.MaxY; changed = true; }
		if (newBounds.MinZ > _minZ) { _minZ = newBounds.MinZ; changed = true; }
		if (newBounds.MaxZ < _maxZ) { _maxZ = newBounds.MaxZ; changed = true; }

		if (changed)
			OnBoundsChanged?.Invoke(oldBounds, CurrentBounds!.Value);
	}
	public bool TryGetSegment(uint segmentId, out ulong[] bricks) => _segments.TryGetValue(segmentId, out bricks);
	public void LoadSegment(uint segmentId, ulong[] bricks)
	{
		if (bricks.Length != SegmentBrickVolume)
			throw new ArgumentException("Invalid segment size");
		_segments[segmentId] = bricks;
		OnSegmentLoaded?.Invoke(segmentId);
		// Update bounds once
		for (int i = 0; i < bricks.Length; i++)
		{
			if (bricks[i] == 0) continue;
			uint px = (uint)(i & 0x3F),
				py = (uint)((i >> 6) & 0x3F),
				pz = (uint)((i >> 12) & 0x3F);
			UpdateBounds(
				x: (ushort)((((segmentId >> 18) & 0x1FF) << 7) | (px << 1)),
				y: (ushort)((((segmentId >> 9) & 0x1FF) << 7) | (py << 1)),
				z: (ushort)(((segmentId & 0x1FF) << 7) | (pz << 1)));
		}
	}
	#endregion Public API
	#region IBrickModel
	/// <summary>
	/// Retrieves the size of the active bounding box along the X axis.
	/// </summary>
	public ushort SizeX => _isEmpty ? (ushort)0 : (ushort)(_maxX - _minX + 2);
	/// <summary>
	/// Retrieves the size of the active bounding box along the Y axis.
	/// </summary>
	public ushort SizeY => _isEmpty ? (ushort)0 : (ushort)(_maxY - _minY + 2);
	/// <summary>
	/// Retrieves the size of the active bounding box along the Z axis.
	/// </summary>
	public ushort SizeZ => _isEmpty ? (ushort)0 : (ushort)(_maxZ - _minZ + 2);
	/// <summary>
	/// Retrieves a brick payload at the given world coordinates.
	/// Snaps coordinates to the nearest even number (brick origin).
	/// </summary>
	public ulong GetBrick(ushort x, ushort y, ushort z) =>
		_segments.TryGetValue(GetSegmentId(x, y, z), out ulong[] bricks)
			? bricks[GetBrickIndex(x, y, z)]
			: 0ul;
	public byte this[ushort x, ushort y, ushort z]
	{
		get => VoxelBrick.GetVoxel(GetBrick(x, y, z), x & 1, y & 1, z & 1);
		set
		{
			// Read-modify-write pattern
			ulong brick = GetBrick(x, y, z);
			int localX = x & 1, localY = y & 1, localZ = z & 1;
			int shift = ((localZ << 2) | (localY << 1) | localX) << 3;
			ulong mask = 0xFFUL << shift;
			ulong newBrick = (brick & ~mask) | ((ulong)value << shift);
			SetBrick(x, y, z, newBrick);
		}
	}
	public IEnumerator<VoxelBrick> GetEnumerator()
	{
		foreach ((ulong[] bricks, uint sz, uint sy, uint sx) in
			// Iterate over all sparse segments
			from KeyValuePair<uint, ulong[]> kvp in _segments
			let segmentId = kvp.Key
			let bricks = kvp.Value// Unpack Segment Coordinates
								  // Segment ID layout: (sx << 18) | (sy << 9) | sz
			let sz = segmentId & 0x1FF
			let sy = (segmentId >> 9) & 0x1FF
			let sx = (segmentId >> 18) & 0x1FF
			select (bricks, sz, sy, sx))
			// Iterate the dense array
			for (uint i = 0; i < bricks.Length; i++)
			{
				ulong payload = bricks[i];
				if (payload == 0) continue; // Skip empty bricks
											// Unpack Brick Index
											// Index layout: px | (py << 6) | (pz << 12)
				uint px = i & 0x3F,
					py = (i >> 6) & 0x3F,
					pz = (i >> 12) & 0x3F;
				yield return new VoxelBrick(
					X: (ushort)((sx << ShiftSegment) | (px << ShiftBrick)),
					Y: (ushort)((sy << ShiftSegment) | (py << ShiftBrick)),
					Z: (ushort)((sz << ShiftSegment) | (pz << ShiftBrick)),
					Payload: payload);
			}
	}
	IEnumerator<Voxel> IEnumerable<Voxel>.GetEnumerator()
	{
		foreach (VoxelBrick brick in this)
			for (int z = 0; z < 2; z++)
				for (int y = 0; y < 2; y++)
					for (int x = 0; x < 2; x++)
						yield return new Voxel(
							X: (ushort)(brick.X + x),
							Y: (ushort)(brick.Y + y),
							Z: (ushort)(brick.Z + z),
							Material: brick.GetVoxel(x, y, z));
	}
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	#endregion IBrickModel
	#region Internal Helpers
	private void UpdateBounds(ushort x, ushort y, ushort z)
	{
		if (_isEmpty)
		{
			_minX = _maxX = x;
			_minY = _maxY = y;
			_minZ = _maxZ = z;
			_isEmpty = false;
			OnBoundsChanged?.Invoke(null, new Bounds(_minX, _maxX, _minY, _maxY, _minZ, _maxZ));
		}
		else
		{
			// Capture old bounds
			ushort oldMinX = _minX, oldMaxX = _maxX,
				oldMinY = _minY, oldMaxY = _maxY,
				oldMinZ = _minZ, oldMaxZ = _maxZ;

			// Update bounds
			if (x < _minX) _minX = x;
			if (x > _maxX) _maxX = x;
			if (y < _minY) _minY = y;
			if (y > _maxY) _maxY = y;
			if (z < _minZ) _minZ = z;
			if (z > _maxZ) _maxZ = z;

			// Fire event if bounds changed
			if (_minX != oldMinX || _maxX != oldMaxX ||
				_minY != oldMinY || _maxY != oldMaxY ||
				_minZ != oldMinZ || _maxZ != oldMaxZ)
			{
				OnBoundsChanged?.Invoke(
					new Bounds(oldMinX, oldMaxX, oldMinY, oldMaxY, oldMinZ, oldMaxZ),
					new Bounds(_minX, _maxX, _minY, _maxY, _minZ, _maxZ));
			}
		}
	}
	private uint GetSegmentId(ushort x, ushort y, ushort z) => PackSegmentId(
		sx: (ushort)(x >> ShiftSegment),
		sy: (ushort)(y >> ShiftSegment),
		sz: (ushort)(z >> ShiftSegment));
	/// <summary>
	/// Pack 9 bits per axis into 27 bits
	/// </summary>
	private uint PackSegmentId(ushort sx, ushort sy, ushort sz) => ((uint)sx << 18) | ((uint)sy << 9) | sz;
	private int GetBrickIndex(ushort x, ushort y, ushort z)
	{
		// Extract bits 1-6 (value 0-63)
		int px = (x >> ShiftBrick) & MaskBrick,
			py = (y >> ShiftBrick) & MaskBrick,
			pz = (z >> ShiftBrick) & MaskBrick;
		// X-major linearization
		return px | (py << 6) | (pz << 12);
	}
	#endregion Internal Helpers
}
