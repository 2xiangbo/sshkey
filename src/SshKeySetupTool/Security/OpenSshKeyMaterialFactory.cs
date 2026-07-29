using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace SshKeySetupTool.Security;

public sealed record KeyMaterial(string PrivateKeyPath, string PublicKeyPath, string PublicKeyLine);

public interface IKeyMaterialFactory
{
    KeyMaterial Create(string privateKeyPath);
}

internal interface IPublicKeyWriter
{
    void Write(string publicKeyPath, string publicKeyLine);
}

public sealed class OpenSshKeyMaterialFactory : IKeyMaterialFactory
{
    private readonly IPublicKeyWriter _publicKeyWriter;

    public OpenSshKeyMaterialFactory()
        : this(new FileSystemPublicKeyWriter())
    {
    }

    internal OpenSshKeyMaterialFactory(IPublicKeyWriter publicKeyWriter)
    {
        _publicKeyWriter = publicKeyWriter ?? throw new ArgumentNullException(nameof(publicKeyWriter));
    }

    public KeyMaterial Create(string privateKeyPath)
    {
        var publicKeyPath = privateKeyPath + ".pub";
        EnsurePathIsUnoccupied(privateKeyPath, publicKeyPath);

        var directory = Path.GetDirectoryName(privateKeyPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var privateKey = new Ed25519PrivateKeyParameters(new SecureRandom());
        var privateKeyText = "-----BEGIN OPENSSH PRIVATE KEY-----\n" +
            Convert.ToBase64String(OpenSshPrivateKeyUtilities.EncodePrivateKey(privateKey), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END OPENSSH PRIVATE KEY-----\n";
        var publicKeyLine = "ssh-ed25519 " +
            Convert.ToBase64String(OpenSshPublicKeyUtilities.EncodePublicKey(privateKey.GeneratePublicKey())) +
            " ssh-key-setup-tool";

        var privateKeyCreated = false;
        try
        {
            using (var privateKeyFile = new FileStream(privateKeyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                privateKeyCreated = true;
                using var privateKeyWriter = new StreamWriter(
                    privateKeyFile,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    bufferSize: 1024,
                    leaveOpen: true);
                privateKeyWriter.Write(privateKeyText);
            }

            ProtectPrivateKey(privateKeyPath);
        }
        catch (UnauthorizedAccessException exception) when (Directory.Exists(privateKeyPath) || Directory.Exists(publicKeyPath))
        {
            DeletePrivateKeyCreatedByThisInvocation(privateKeyCreated, privateKeyPath);
            throw new IOException("The selected private-key path or matching public-key path already exists. Choose a new path.", exception);
        }
        catch
        {
            DeletePrivateKeyCreatedByThisInvocation(privateKeyCreated, privateKeyPath);
            throw;
        }

        try
        {
            _publicKeyWriter.Write(publicKeyPath, publicKeyLine);
        }
        catch (Exception exception)
        {
            throw new IOException(
                $"The public key could not be written. The protected private key remains at '{privateKeyPath}' for recovery.",
                exception);
        }

        return new KeyMaterial(privateKeyPath, publicKeyPath, publicKeyLine);
    }

    private static void EnsurePathIsUnoccupied(string privateKeyPath, string publicKeyPath)
    {
        if (File.Exists(privateKeyPath) || Directory.Exists(privateKeyPath) ||
            File.Exists(publicKeyPath) || Directory.Exists(publicKeyPath))
        {
            throw new IOException("The selected private-key path or matching public-key path already exists. Choose a new path.");
        }
    }

    private static void DeletePrivateKeyCreatedByThisInvocation(bool privateKeyCreated, string privateKeyPath)
    {
        if (privateKeyCreated)
        {
            File.Delete(privateKeyPath);
        }
    }

    private static void ProtectPrivateKey(string privateKeyPath)
    {
        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current Windows user could not be resolved.");
        var privateKeyFile = new FileInfo(privateKeyPath);
        var security = privateKeyFile.GetAccessControl();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
        privateKeyFile.SetAccessControl(security);
    }

    private sealed class FileSystemPublicKeyWriter : IPublicKeyWriter
    {
        public void Write(string publicKeyPath, string publicKeyLine)
        {
            using var publicKeyFile = new FileStream(publicKeyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var publicKeyWriter = new StreamWriter(
                publicKeyFile,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                bufferSize: 1024,
                leaveOpen: true);
            publicKeyWriter.Write(publicKeyLine + Environment.NewLine);
        }
    }
}
