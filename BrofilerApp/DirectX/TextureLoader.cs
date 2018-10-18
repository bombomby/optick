using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System.IO;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct2D1.RenderTarget;
using PixelFormat = SharpDX.WIC.PixelFormat;

namespace Profiler.DirectX
{
	public static class TextureLoader
	{
		private static readonly ImagingFactory Imgfactory = new ImagingFactory();

		public static SharpDX.Direct2D1.Bitmap LoadBitmap(Stream stream, DeviceContext context)
		{
			/*
                        var props = new BitmapProperties1
                        {
                            PixelFormat = new SharpDX.Direct2D1.PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)
                        };
             */

			return SharpDX.Direct2D1.Bitmap.FromWicBitmap(context, LoadBitmap(stream));
		}

		private static BitmapSource LoadBitmap(Stream stream)
		{
			var d = new BitmapDecoder(
				Imgfactory,
				stream,
				DecodeOptions.CacheOnDemand
				);

			var frame = d.GetFrame(0);

			var fconv = new FormatConverter(Imgfactory);

			fconv.Initialize(
				frame,
				PixelFormat.Format32bppPRGBA,
				BitmapDitherType.None, null,
				0.0, BitmapPaletteType.Custom);
			return fconv;
		}

		public static Texture2D CreateTex2DFromFile(Device device, Stream stream)
		{
			var bSource = LoadBitmap(stream);
			return CreateTex2DFromBitmap(device, bSource);
		}

		public static Texture2D CreateTex2DFromBitmap(Device device, BitmapSource bsource)
		{

			Texture2DDescription desc;
			desc.Width = bsource.Size.Width;
			desc.Height = bsource.Size.Height;
			desc.ArraySize = 1;
			desc.BindFlags = BindFlags.ShaderResource;
			desc.Usage = ResourceUsage.Default;
			desc.CpuAccessFlags = CpuAccessFlags.None;
			desc.Format = Format.R8G8B8A8_UNorm;
			desc.MipLevels = 1;
			desc.OptionFlags = ResourceOptionFlags.None;
			desc.SampleDescription.Count = 1;
			desc.SampleDescription.Quality = 0;

			var s = new DataStream(bsource.Size.Height * bsource.Size.Width * 4, true, true);
			bsource.CopyPixels(bsource.Size.Width * 4, s);

			var rect = new DataRectangle(s.DataPointer, bsource.Size.Width * 4);

			var t2D = new Texture2D(device, desc, rect);

			return t2D;
		}
	}
}