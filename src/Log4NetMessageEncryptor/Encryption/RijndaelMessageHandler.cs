using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;

namespace ArtisanCode.Log4NetMessageEncryptor.Encryption
{
    public abstract class RijndaelMessageHandler
    {
        public const string CONFIGURATION_SECTION_NAME = "Log4NetMessageEncryption";

        public const string CYPHER_TEXT_IV_SEPERATOR = ">>";

        /// <summary>
        /// Initializes a new instance of the <see cref="RijndaelMessageEncryptor"/> class.
        /// </summary>
        /// <remarks>
        /// Reads the configuration directly from the configuration file section: Log4NetMessageEncryption
        /// </remarks>
        public RijndaelMessageHandler()
        {
            Configuration = ConfigurationManager.GetSection(CONFIGURATION_SECTION_NAME) as Log4NetMessageEncryptorConfiguration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RijndaelMessageEncryptor"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public RijndaelMessageHandler(Log4NetMessageEncryptorConfiguration config)
        {
            Configuration = config;
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Log4NetMessageEncryptorConfiguration Configuration { get; set; }
        
        /// <summary>
        /// Configures the crypto container.
        /// </summary>
        /// <param name="cryptoContainer">The crypto container to configure.</param>
        /// <param name="config">The configuration to use during encryption.</param>
        public virtual void ConfigureCryptoContainer(RijndaelManaged cryptoContainer, Log4NetMessageEncryptorConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config", "The whole encryption configuration is null. Have you forgotten to add it to the config section: " + CONFIGURATION_SECTION_NAME);
            }

            if (config.EncryptionKey == null)
            {
                throw new ArgumentException("config", "The encryption key configuration is null. Have you forgotten to add it to the config section: " + CONFIGURATION_SECTION_NAME);
            }

            if (string.IsNullOrWhiteSpace(config.EncryptionKey.Key))
            {
                throw new CryptographicException("Encryption key is missing. Have you forgotten to add it to the config section: " + CONFIGURATION_SECTION_NAME);
            }

            if (!cryptoContainer.LegalKeySizes.Any(x => (x.MinSize <= config.EncryptionKey.KeySize) && (config.EncryptionKey.KeySize <= x.MaxSize)))
            {
                throw new CryptographicException("Invalid Key Size specified. The recommended value is: 256");
            }

            byte[] key = Convert.FromBase64String(config.EncryptionKey.Key);

            // Check that the key length is equal to config.KeySize / 8
            // e.g. 256/8 == 32 bytes expected for the key
            if (key.Length != (config.EncryptionKey.KeySize / 8))
            {
                throw new CryptographicException("Encryption key is the wrong length. Please ensure that it is *EXACTLY* " + config.EncryptionKey.KeySize + " bits long");
            }

            cryptoContainer.Mode = config.CipherMode;
            cryptoContainer.Padding = config.Padding;
            cryptoContainer.KeySize = config.EncryptionKey.KeySize;
            cryptoContainer.Key = key;

            // Generate a new Unique IV for this container and transaction (can be overridden later to decrypt messages where the IV is known)
            cryptoContainer.GenerateIV();
        }
    }
}
