namespace Voxel2Pixel.Render
{
	/// <summary>
	/// An ITriangleRenderer draws triangles where each triangle represents half of one of the diamonds making up one of the three visible faces of a voxel cube from an isometric perspective
	/// </summary>
	public interface ITriangleRenderer : IVoxelRenderer
	{
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing left
		/// </summary>
		void DrawLeftTriangle(int x, int y, int color);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing right
		/// </summary>
		void DrawRightTriangle(int x, int y, int color);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing left, representing the visible vertical face of voxel
		/// </summary>
		void DrawLeftTriangleVerticalFace(int x, int y, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing left, representing the left face of voxel
		/// </summary>
		void DrawLeftTriangleLeftFace(int x, int y, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing left, representing the right face of voxel
		/// </summary>
		void DrawLeftTriangleRightFace(int x, int y, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing right representing the visible vertical face of voxel
		/// </summary>
		void DrawRightTriangleVerticalFace(int x, int y, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing right representing the left face of voxel
		/// </summary>
		void DrawRightTriangleLeftFace(int x, int y, byte voxel);
		/// <summary>
		/// Draws a triangle 3 high and 2 wide pointing right representing the right face of voxel
		/// </summary>
		void DrawRightTriangleRightFace(int x, int y, byte voxel);
	}
}
