using System.Security.Cryptography;

namespace SmartDine.Domain.Interfaces;

public interface IRsaKeyProvider
{
    RSAParameters PrivateKeyParameters { get; }
    RSAParameters PublicKeyParameters { get; }
}
