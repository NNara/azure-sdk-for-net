﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Threading;
using Azure.Core;

namespace Azure.Security.KeyVault.Keys.Cryptography
{
    internal class RsaCryptographyProvider : LocalCryptographyProvider
    {
        internal RsaCryptographyProvider(KeyVaultKey key) : base(key)
        {
        }

        public override bool SupportsOperation(KeyOperation operation)
        {
            if (KeyMaterial != null)
            {
                if (operation == KeyOperation.Encrypt || operation == KeyOperation.Decrypt || operation == KeyOperation.Sign || operation == KeyOperation.Verify || operation == KeyOperation.WrapKey || operation == KeyOperation.UnwrapKey)
                {
                    return KeyMaterial.SupportsOperation(operation);
                }
            }

            return false;
        }

        public override EncryptResult Encrypt(EncryptionAlgorithm algorithm, byte[] plaintext, CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(plaintext, nameof(plaintext));

            ThrowIfTimeInvalid();

            RSAEncryptionPadding padding = algorithm.GetRsaEncryptionPadding();
            byte[] ciphertext = Encrypt(plaintext, padding);

            EncryptResult result = null;

            if (ciphertext != null)
            {
                result = new EncryptResult
                {
                    Algorithm = algorithm,
                    Ciphertext = ciphertext,
                    KeyId = KeyMaterial.Id,
                };
            }

            return result;
        }

        public override DecryptResult Decrypt(EncryptionAlgorithm algorithm, byte[] ciphertext, CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(ciphertext, nameof(ciphertext));

            RSAEncryptionPadding padding = algorithm.GetRsaEncryptionPadding();
            byte[] plaintext = Decrypt(ciphertext, padding);

            DecryptResult result = null;

            if (plaintext != null)
            {
                result = new DecryptResult
                {
                    Algorithm = algorithm,
                    KeyId = KeyMaterial.Id,
                    Plaintext = plaintext,
                };
            }

            return result;
        }

        public override SignResult Sign(SignatureAlgorithm algorithm, byte[] digest, CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(digest, nameof(digest));

            ThrowIfTimeInvalid();

            // A private key is required to sign. Send to the server.
            if (MustRemote)
            {
                // TODO: Log that we need a private key.
                return null;
            }

            HashAlgorithmName hashAlgorithm = algorithm.GetHashAlgorithmName();
            if (hashAlgorithm == default)
            {
                // TODO: Log that we don't support the given algorithm.
                return null;
            }

            RSASignaturePadding padding = algorithm.GetRsaSignaturePadding();
            if (padding is null)
            {
                // TODO: Log that we don't support the given algorithm.
                return null;
            }

            using RSA rsa = KeyMaterial.ToRSA(true);
            byte[] signature = rsa.SignHash(digest, hashAlgorithm, padding);

            return new SignResult
            {
                Algorithm = algorithm,
                KeyId = KeyMaterial.Id,
                Signature = signature,
            };
        }

        public override VerifyResult Verify(SignatureAlgorithm algorithm, byte[] digest, byte[] signature, CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(digest, nameof(digest));
            Argument.AssertNotNull(signature, nameof(signature));

            HashAlgorithmName hashAlgorithm = algorithm.GetHashAlgorithmName();
            if (hashAlgorithm == default)
            {
                // TODO: Log that we don't support the given algorithm.
                return null;
            }

            RSASignaturePadding padding = algorithm.GetRsaSignaturePadding();
            if (padding is null)
            {
                // TODO: Log that we don't support the given algorithm.
                return null;
            }

            using RSA rsa = KeyMaterial.ToRSA();
            bool isValid = rsa.VerifyHash(digest, signature, hashAlgorithm, padding);

            return new VerifyResult
            {
                Algorithm = algorithm,
                IsValid = isValid,
                KeyId = KeyMaterial.Id,
            };
        }

        public override WrapResult WrapKey(KeyWrapAlgorithm algorithm, byte[] key, CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(key, nameof(key));

            ThrowIfTimeInvalid();

            RSAEncryptionPadding padding = algorithm.GetRsaEncryptionPadding();
            byte[] encryptedKey = Encrypt(key, padding);

            WrapResult result = null;

            if (encryptedKey != null)
            {
                result = new WrapResult
                {
                    Algorithm = algorithm,
                    EncryptedKey = encryptedKey,
                    KeyId = KeyMaterial.Id,
                };
            }

            return result;
        }

        public override UnwrapResult UnwrapKey(KeyWrapAlgorithm algorithm, byte[] encryptedKey, CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(encryptedKey, nameof(encryptedKey));

            RSAEncryptionPadding padding = algorithm.GetRsaEncryptionPadding();
            byte[] key = Decrypt(encryptedKey, padding);

            UnwrapResult result = null;

            if (key != null)
            {
                result = new UnwrapResult
                {
                    Algorithm = algorithm,
                    Key = key,
                    KeyId = KeyMaterial.Id,
                };
            }

            return result;
        }

        private byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (padding is null)
            {
                // TODO: Log that we don't support the given algorithm.
                return null;
            }

            using RSA rsa = KeyMaterial.ToRSA(true);
            return rsa.Encrypt(data, padding);
        }

        private byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            // A private key is required to decrypt. Send to the server.
            if (MustRemote)
            {
                // TODO: Log that we need a private key.
                return null;
            }

            if (padding is null)
            {
                // TODO: Log that we don't support the given algorithm.
                return null;
            }

            using RSA rsa = KeyMaterial.ToRSA();
            return rsa.Decrypt(data, padding);
        }
    }
}
