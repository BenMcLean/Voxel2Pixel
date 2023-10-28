using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using Voxel2Pixel.Color;
using Voxel2Pixel.Draw;
using Voxel2Pixel.Interfaces;
using Voxel2Pixel.Model;
using Voxel2Pixel.Render;
using Voxel2PixelTest.Model;
using Xunit;

namespace Voxel2PixelTest.Render
{
    public class AnimatedTest
    {
        [Fact]
        public void GifTest()
        {
            ArrayModel model = new ArrayModel(
                sizeX: 7,
                sizeY: 4,
                sizeZ: 7);
            int width = VoxelDraw.IsoWidth(model),
                height = VoxelDraw.IsoHeight(model);
            Random random = new Random();
            IVoxelColor voxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
            List<byte[]> frames = new List<byte[]>();
            for (int x = model.SizeX - 1; x >= 0; x--)
                for (int y = model.SizeY - 1; y >= 0; y--)
                    for (int z = 0; z < model.SizeZ; z++)
                    {
                        model.Array[x][y][z] = 1;
                        int randomX = random.Next(0, model.SizeX),
                            randomY = random.Next(0, model.SizeY),
                            randomZ = random.Next(0, model.SizeZ);
                        model.Array[randomX][randomY][randomZ] = (byte)(random.Next(0, ArrayModelTest.Rainbow.Count) + 1);
                        ArrayRenderer arrayRenderer = new ArrayRenderer
                        {
                            Image = new byte[width * 4 * height],
                            Width = width,
                            VoxelColor = voxelColor,
                        };
                        VoxelDraw.Iso(model, arrayRenderer);
                        frames.Add(arrayRenderer.Image);
                        model.Array[randomX][randomY][randomZ] = 0;
                        model.Array[x][y][z] = 2;
                    }
            ImageMaker.AnimatedGif(
                width: width,
                frames: frames.ToArray())
                .SaveAsGif("AnimatedTest.gif");
        }
        [Fact]
        public void SizeTest()
        {
            EmptyModel empty = new EmptyModel
            {
                SizeX = 8,
                SizeY = 8,
                SizeZ = 8,
            };
            int width = VoxelDraw.IsoWidth(empty),
                height = VoxelDraw.IsoHeight(empty),
                start = 6;
            IVoxelColor iVoxelColor = new NaiveDimmer(ArrayModelTest.RainbowPalette);
            List<byte[]> frames = new List<byte[]>();
            for (int sizeX = start; sizeX <= empty.SizeX; sizeX++)
                for (int sizeY = start; sizeY <= empty.SizeY; sizeY++)
                    for (int sizeZ = start; sizeZ <= empty.SizeZ; sizeZ++)
                    {
                        ArrayRenderer arrayRenderer = new ArrayRenderer
                        {
                            Image = new byte[width * 4 * height],
                            Width = width,
                            VoxelColor = iVoxelColor,
                        };
                        IModel model = new ArrayModel(ArrayModelTest.RainbowBox(
                                sizeX: sizeX,
                                sizeY: sizeY,
                                sizeZ: sizeZ));
                        VoxelDraw.Iso(
                            model: model,
                            renderer: arrayRenderer);
                        VoxelDraw.IsoLocate(
                            out int pixelX,
                            out int pixelY,
                            model: model,
                            voxelX: 0,
                            voxelY: 0,
                            voxelZ: model.SizeZ - 1);
                        arrayRenderer.Rect(
                            x: pixelX,
                            y: pixelY,
                            color: 0xFFFFFFFF);
                        frames.Add(arrayRenderer.Image);
                    }
            ImageMaker.AnimatedGif(
                scaleX: 16,
                scaleY: 16,
                width: width,
                frameDelay: 50,
                frames: frames.ToArray())
                .SaveAsGif("SizeTest.gif");
        }
    }
}
