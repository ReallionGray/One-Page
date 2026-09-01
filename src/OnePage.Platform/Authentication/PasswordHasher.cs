using System.Security.Cryptography;

namespace OnePage.Platform;

/// <summary>
/// Secure password hashing service using PBKDF2
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    bool VerifyPassword(string password, string hashedPassword);
    
    /// <summary>
    /// Check if a password needs rehashing (e.g., due to updated parameters)
    /// </summary>
    bool NeedsRehash(string hashedPassword);
}

/// <summary>
/// PBKDF2-based password hasher
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private readonly int _iterations;
    private readonly int _saltSize;
    private readonly int _hashSize;
    
    public Pbkdf2PasswordHasher(int iterations = 100000, int saltSize = 16, int hashSize = 20)
    {
        _iterations = iterations;
        _saltSize = saltSize;
        _hashSize = hashSize;
    }
    
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        
        // Generate a random salt
        var salt = new byte[_saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        
        // Compute the hash
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _iterations);
        var hash = pbkdf2.GetBytes(_hashSize);
        
        // Combine salt, iterations, and hash for storage
        var hashWithSalt = new byte[_saltSize + 4 + _hashSize];
        Buffer.BlockCopy(salt, 0, hashWithSalt, 0, _saltSize);
        Buffer.BlockCopy(BitConverter.GetBytes(_iterations), 0, hashWithSalt, _saltSize, 4);
        Buffer.BlockCopy(hash, 0, hashWithSalt, _saltSize + 4, _hashSize);
        
        return Convert.ToBase64String(hashWithSalt);
    }
    
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;
        
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;
        
        try
        {
            var hashWithSalt = Convert.FromBase64String(hashedPassword);
            if (hashWithSalt.Length < _saltSize + 4 + _hashSize)
                return false;
            
            // Extract salt, iterations, and hash
            var salt = new byte[_saltSize];
            Buffer.BlockCopy(hashWithSalt, 0, salt, 0, _saltSize);
            
            var storedIterations = BitConverter.ToInt32(hashWithSalt, _saltSize);
            
            var hash = new byte[_hashSize];
            Buffer.BlockCopy(hashWithSalt, _saltSize + 4, hash, 0, _hashSize);
            
            // Compute hash with the same salt and iterations
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, storedIterations);
            var computedHash = pbkdf2.GetBytes(_hashSize);
            
            // Compare in constant time
            return CryptographicOperations.FixedTimeEquals(computedHash, hash);
        }
        catch
        {
            return false;
        }
    }
    
    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;
        
        try
        {
            var hashWithSalt = Convert.FromBase64String(hashedPassword);
            if (hashWithSalt.Length < _saltSize + 4)
                return true;
            
            var storedIterations = BitConverter.ToInt32(hashWithSalt, _saltSize);
            return storedIterations != _iterations;
        }
        catch
        {
            return true;
        }
    }
}


