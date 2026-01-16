namespace BenVoxel.Interfaces;

public interface IEditableBrickModel : IBrickModel, IEditableModel
{
	/// <summary>
	/// Sets a brick payload at the given world coordinates.
	/// Implementation should snap x,y,z to the nearest brick origin (multiple of 2)
	/// internally using: x &amp; ~1, y &amp; ~1, z &amp; ~1 (or x &amp; 0xFFFE, etc.)
	/// </summary>
	void SetBrick(ushort x, ushort y, ushort z, ulong payload);
}
