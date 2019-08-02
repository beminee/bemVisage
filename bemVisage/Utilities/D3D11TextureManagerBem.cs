using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Ensage.SDK.Renderer.DX11;
using Ensage.SDK.VPK;
using NLog;
using PlaySharp.Toolkit.Helper;
using Rectangle = System.Drawing.Rectangle;

namespace bemVisage.Utilities
{
    public static class D3D11TextureManagerBem
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static D3D11TextureManager textureManager;

        private static D3D11TextureManager TextureManager
        {
            get
            {
                if (textureManager == null)
                {
                    textureManager = IoC.Get<D3D11TextureManager>();
                }

                return textureManager;
            }
        }

        private static VpkBrowser vpkBrowser;

        private static VpkBrowser VpkBrowser
        {
            get
            {
                if (vpkBrowser == null)
                {
                    vpkBrowser = IoC.Get<VpkBrowser>();
                }

                return vpkBrowser;
            }
        }

        public static void LoadFromDota(string textureKey, string file)
        {
            if (TextureManager.GetTexture(textureKey) != null)
            {
                return;
            }

            var bitmapStream = VpkBrowser.FindImage(file);
            if (bitmapStream != null)
            {
                FromStream(textureKey, bitmapStream);
            }

            bitmapStream = VpkBrowser.FindImage(@"panorama\images\spellicons\invoker_empty1_png.vtex_c");
            if (bitmapStream != null)
            {
                FromStream(textureKey, bitmapStream);
            }
        }

        public static void LoadFromResource(string textureKey, string file, Assembly assembly = null)
        {
            if (TextureManager.GetTexture(textureKey) != null)
            {
                return;
            }

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            var resourceFile = assembly.GetManifestResourceNames().FirstOrDefault(f => f.EndsWith(file));
            if (resourceFile == null)
            {
                throw new ArgumentNullException(nameof(resourceFile));
            }

            using (var ms = new MemoryStream())
            {
                assembly.GetManifestResourceStream(resourceFile)?.CopyTo(ms);
                FromStream(textureKey, ms);
            }
        }

        private static void FromStream(string textureKey, Stream stream)
        {
            LoadFromBitmap(textureKey, new Bitmap(stream));
        }

        public static void LoadFromStream(string textureKey, Stream stream)
        {
            var texture = TextureManager.GetTexture(textureKey);
            if (texture != null)
            {
                return;
            }

            FromStream(textureKey, stream);
        }

        public static void LoadFromBitmap(string textureKey, Bitmap bitmap)
        {
            if (TextureManager.GetTexture(textureKey) == null)
            {

                var width = bitmap.Width;
                var height = bitmap.Height;

                var imageAttributes = new ImageAttributes();
                imageAttributes.SetGamma(2, ColorAdjustType.Bitmap);

                Graphics.FromImage(bitmap).DrawImage(bitmap, new Rectangle(0, 0, width, height), 0, 0, width, height,
                    GraphicsUnit.Pixel, imageAttributes);

                var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                TextureManager.LoadFromStream(textureKey, stream);
            }
            return;
        }
    }
}