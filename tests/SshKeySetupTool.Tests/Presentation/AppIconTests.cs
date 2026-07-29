using SshKeySetupTool;
using System.Buffers.Binary;
using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace SshKeySetupTool.Tests.Presentation;

public sealed class AppIconTests
{
    private const string IconResourceName =
        "SshKeySetupTool.Assets.ssh-key-tool-icon.ico";

    [Fact]
    public void Load_ReturnsTheEmbeddedApplicationIcon()
    {
        using var icon = AppIcon.Load();
        using var bitmap = icon.ToBitmap();

        Assert.InRange(icon.Width, 16, 256);
        Assert.Equal(icon.Width, icon.Height);
        Assert.Equal(icon.Size, bitmap.Size);
    }

    [Fact]
    public void Form1_UsesEmbeddedApplicationIconAndClearsOwnedIconOnDispose()
    {
        RunInSta(() =>
        {
            using var expectedIcon = AppIcon.Load();
            using var expectedBitmap = expectedIcon.ToBitmap();
            using var form = new Form1();
            var ownedIcon = typeof(Form1).GetField(
                "_applicationIcon",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var formIcon = form.Icon;

            Assert.NotNull(ownedIcon);
            Assert.NotNull(formIcon);
            using var formBitmap = formIcon.ToBitmap();
            Assert.Equal(HashPixels(expectedBitmap), HashPixels(formBitmap));

            form.Dispose();

            Assert.Null(ownedIcon.GetValue(form));
        });
    }

    [Fact]
    public void EmbeddedApplicationIcon_ContainsSevenContiguousPngLayers()
    {
        var iconBytes = ReadEmbeddedIconBytes();
        var expectedSizes = new[] { 16, 24, 32, 48, 64, 128, 256 };

        Assert.True(iconBytes.Length >= 6);
        Assert.Equal((ushort)0, ReadUInt16(iconBytes, 0));
        Assert.Equal((ushort)1, ReadUInt16(iconBytes, 2));
        Assert.Equal((ushort)expectedSizes.Length, ReadUInt16(iconBytes, 4));

        var expectedOffset = 6 + (16 * expectedSizes.Length);
        for (var index = 0; index < expectedSizes.Length; index++)
        {
            var entryOffset = 6 + (16 * index);
            Assert.True(iconBytes.Length >= entryOffset + 16);

            var width = iconBytes[entryOffset] == 0 ? 256 : iconBytes[entryOffset];
            var height = iconBytes[entryOffset + 1] == 0 ? 256 : iconBytes[entryOffset + 1];
            var payloadLength = ReadUInt32(iconBytes, entryOffset + 8);
            var payloadOffset = ReadUInt32(iconBytes, entryOffset + 12);

            Assert.Equal(expectedSizes[index], width);
            Assert.Equal(expectedSizes[index], height);
            Assert.Equal((ushort)1, ReadUInt16(iconBytes, entryOffset + 4));
            Assert.Equal((ushort)32, ReadUInt16(iconBytes, entryOffset + 6));
            Assert.Equal((uint)expectedOffset, payloadOffset);
            Assert.True(payloadLength > 8);
            Assert.True(payloadOffset + payloadLength <= iconBytes.Length);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
                iconBytes.AsSpan((int)payloadOffset, 8).ToArray());

            using var payload = new MemoryStream(
                iconBytes,
                (int)payloadOffset,
                (int)payloadLength,
                writable: false);
            using var layer = Image.FromStream(payload, useEmbeddedColorManagement: false, validateImageData: true);
            Assert.Equal(expectedSizes[index], layer.Width);
            Assert.Equal(expectedSizes[index], layer.Height);

            expectedOffset += (int)payloadLength;
        }

        Assert.Equal(iconBytes.Length, expectedOffset);
    }

    private static string HashPixels(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private static byte[] ReadEmbeddedIconBytes()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(IconResourceName);
        Assert.NotNull(stream);
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    private static ushort ReadUInt16(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, sizeof(ushort)));

    private static uint ReadUInt32(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception caught)
            {
                exception = caught;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
