namespace Voxel2Pixel.Render
{
	/// <summary>
	/// An ITriangleRenderer draws triangles where each triangle represents half of one of the diamonds making up one of the three visible faces of a voxel cube from an isometric perspective
	/// </summary>
	public interface ITriangleRenderer : IVoxelRenderer
	{
		/// <summary>
		/// Draws a triangle 3 high and 2 wide
		/// </summary>
		/// <param name="right">Points right if true, else points left</param>
		void Triangle(int x, int y, bool right, uint color);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide, representing the visible vertical face of voxel
		/// </summary>
		/// <param name="right">Points right if true, else points left</param>
		void TriangleVerticalFace(int x, int y, bool right, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide, representing the left face of voxel
		/// </summary>
		/// <param name="right">Points right if true, else points left</param>
		void TriangleLeftFace(int x, int y, bool right, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide, representing the right face of voxel
		/// </summary>
		/// <param name="right">Points right if true, else points left</param>
		void TriangleRightFace(int x, int y, bool right, byte voxel);
	}
}
