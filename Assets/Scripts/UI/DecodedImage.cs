using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UI
{
    public class DecodedImage : IDisposable
    {
        public readonly int Height;
        public readonly int Width;

        public Sprite _sprite;

        private Texture2D _texture;
        public byte[] PixelData;

        public DecodedImage(Image<Rgba32> image)
        {
            PixelData = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(PixelData);

            var rowSize = image.Width * 4;
            var temp = new byte[rowSize];

            for (var y = 0; y < image.Height / 2; y++)
            {
                var top = y * rowSize;
                var bottom = (image.Height - 1 - y) * rowSize;

                Buffer.BlockCopy(PixelData, top, temp, 0, rowSize);
                Buffer.BlockCopy(PixelData, bottom, PixelData, top, rowSize);
                Buffer.BlockCopy(temp, 0, PixelData, bottom, rowSize);
            }

            Width = image.Width;
            Height = image.Height;
        }

        public void Dispose()
        {
            Object.Destroy(_sprite);
            _sprite = null;
            Object.Destroy(_texture);
            _texture = null;
            PixelData = null;
        }

        public Sprite GetSprite()
        {
            if (!_sprite)
                _sprite = Texture2DToSprite(ToTexture2D());

            return _sprite;
        }

        private Texture2D ToTexture2D()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

            texture.SetPixelData(PixelData, 0);
            texture.Apply(false, false);

            return texture;
        }

        private static Sprite Texture2DToSprite(Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            return sprite;
        }
    }
}