using Microsoft.UI.Xaml;

namespace Voxel2Pixel.Uno;

public class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		Microsoft.UI.Xaml.Application.Start((p) => new App());
	}
}
