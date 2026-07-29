using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using SshKeySetupTool.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SshKeySetupTool.Tests.Security;

public sealed class OpenSshKeyMaterialFactoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void Create_WritesOpenSshEd25519KeyPair()
    {
        Directory.CreateDirectory(_directory);

        var privateKeyPath = Path.Combine(_directory, "id_ed25519");
        var material = new OpenSshKeyMaterialFactory().Create(privateKeyPath);

        Assert.StartsWith("-----BEGIN OPENSSH PRIVATE KEY-----", File.ReadAllText(privateKeyPath));
        Assert.StartsWith("ssh-ed25519 ", File.ReadAllText(material.PublicKeyPath));
        Assert.Equal(material.PublicKeyLine, File.ReadAllText(material.PublicKeyPath).Trim());
    }

    [Fact]
    public void Create_RefusesExistingPrivateKey()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");
        const string existingPrivateKey = "existing private key";
        File.WriteAllText(privateKeyPath, existingPrivateKey);

        Assert.Throws<IOException>(() => new OpenSshKeyMaterialFactory().Create(privateKeyPath));
        Assert.Equal(existingPrivateKey, File.ReadAllText(privateKeyPath));
        Assert.False(File.Exists(privateKeyPath + ".pub"));
    }

    [Fact]
    public void Create_RefusesExistingPublicKeyBeforeWritingPrivateKey()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");
        const string existingPublicKey = "existing public key";
        File.WriteAllText(privateKeyPath + ".pub", existingPublicKey);

        Assert.Throws<IOException>(() => new OpenSshKeyMaterialFactory().Create(privateKeyPath));
        Assert.False(File.Exists(privateKeyPath));
        Assert.Equal(existingPublicKey, File.ReadAllText(privateKeyPath + ".pub"));
    }

    [Fact]
    public void Create_RefusesPrivateKeyDirectory()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");
        Directory.CreateDirectory(privateKeyPath);

        var exception = Record.Exception(() => new OpenSshKeyMaterialFactory().Create(privateKeyPath));

        Assert.IsType<IOException>(exception);
        Assert.True(Directory.Exists(privateKeyPath));
        Assert.False(File.Exists(privateKeyPath + ".pub"));
    }

    [Fact]
    public void Create_RefusesPublicKeyDirectoryWithoutLeavingPrivateKey()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");
        var publicKeyPath = privateKeyPath + ".pub";
        Directory.CreateDirectory(publicKeyPath);

        var exception = Record.Exception(() => new OpenSshKeyMaterialFactory().Create(privateKeyPath));

        Assert.False(File.Exists(privateKeyPath));
        Assert.IsType<IOException>(exception);
        Assert.True(Directory.Exists(publicKeyPath));
    }

    [Fact]
    public void Create_CreatesMissingParentDirectory()
    {
        var privateKeyPath = Path.Combine(_directory, "keys", "id_ed25519");

        var material = new OpenSshKeyMaterialFactory().Create(privateKeyPath);

        Assert.True(File.Exists(material.PrivateKeyPath));
        Assert.True(File.Exists(material.PublicKeyPath));
    }

    [Fact]
    public void Create_RoundTripsPrivateKeyAndMatchesPublicMaterial()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");

        var material = new OpenSshKeyMaterialFactory().Create(privateKeyPath);

        var privateKeyBytes = Convert.FromBase64String(string.Concat(
            File.ReadAllLines(material.PrivateKeyPath).Where(line => !line.StartsWith("-----", StringComparison.Ordinal))));
        var privateKey = Assert.IsType<Ed25519PrivateKeyParameters>(
            OpenSshPrivateKeyUtilities.ParsePrivateKeyBlob(privateKeyBytes));
        var publicKeyBytes = Convert.FromBase64String(File.ReadAllText(material.PublicKeyPath).Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
        var publicKey = Assert.IsType<Ed25519PublicKeyParameters>(OpenSshPublicKeyUtilities.ParsePublicKey(publicKeyBytes));

        Assert.Equal(OpenSshPublicKeyUtilities.EncodePublicKey(privateKey.GeneratePublicKey()), publicKeyBytes);
        Assert.Equal(privateKey.GeneratePublicKey().GetEncoded(), publicKey.GetEncoded());
    }

    [Fact]
    public void Create_RestrictsPrivateKeyAclToCurrentUser()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");

        new OpenSshKeyMaterialFactory().Create(privateKeyPath);

        var security = new FileInfo(privateKeyPath).GetAccessControl(AccessControlSections.Access);
        var currentUser = WindowsIdentity.GetCurrent().User;
        var rules = security.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
            .OfType<FileSystemAccessRule>();

        Assert.True(security.AreAccessRulesProtected);
        Assert.NotNull(currentUser);
        Assert.Contains(rules, rule =>
            rule.IdentityReference == currentUser &&
            rule.AccessControlType == AccessControlType.Allow &&
            (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl);
    }

    [Fact]
    public void Create_WhenPublicKeyWriteFails_RetainsProtectedPrivateKeyAndReplacementPublicKey()
    {
        Directory.CreateDirectory(_directory);
        var privateKeyPath = Path.Combine(_directory, "id_ed25519");
        const string replacementPublicKey = "another process's public key";
        var publicKeyWriter = new FailingPublicKeyWriter(replacementPublicKey);
        var factory = new OpenSshKeyMaterialFactory(publicKeyWriter);

        var exception = Assert.Throws<IOException>(() => factory.Create(privateKeyPath));

        Assert.Contains(privateKeyPath, exception.Message, StringComparison.Ordinal);
        Assert.Same(publicKeyWriter.Failure, exception.InnerException);
        Assert.True(File.Exists(privateKeyPath));
        Assert.Equal(replacementPublicKey, File.ReadAllText(privateKeyPath + ".pub"));

        var security = new FileInfo(privateKeyPath).GetAccessControl(AccessControlSections.Access);
        var currentUser = WindowsIdentity.GetCurrent().User;
        var rules = security.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
            .OfType<FileSystemAccessRule>();

        Assert.True(security.AreAccessRulesProtected);
        Assert.NotNull(currentUser);
        Assert.Contains(rules, rule =>
            rule.IdentityReference == currentUser &&
            rule.AccessControlType == AccessControlType.Allow &&
            (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl);
    }

    [Fact]
    public void PublicKeyWriterSeam_IsNotPublicAndFactoryOnlyExposesDefaultConstruction()
    {
        Assert.False(typeof(IPublicKeyWriter).IsPublic);

        var constructor = Assert.Single(typeof(OpenSshKeyMaterialFactory).GetConstructors());
        Assert.Empty(constructor.GetParameters());
    }

    private sealed class FailingPublicKeyWriter(string replacementPublicKey) : IPublicKeyWriter
    {
        public IOException Failure { get; } = new("Simulated public-key write failure.");

        public void Write(string publicKeyPath, string publicKeyLine)
        {
            File.WriteAllText(publicKeyPath, replacementPublicKey);
            throw Failure;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
