using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyMongo
{
    /// <summary>
    /// The connection string builder.
    /// </summary>
    public class ConnectionStringBuilder
    {
        private string _connectionString;

        public override string ToString()
        {
            return _connectionString;
        }

        private const string DefaultDatabase = "admin";
        private const int DefaultPort = 27017;
        private const string Protocol = "mongodb://";

        private static readonly IDictionary<string, Action<string, ConnectionStringBuilder>> OptionsHandler = new Dictionary<string, Action<string, ConnectionStringBuilder>>
              {
                  {"strict", (v, b) => b.SetStrictMode(bool.Parse(v))},
                  {"querytimeout", (v, b) => b.SetQueryTimeout(int.Parse(v))},
                  {"pooling", (v, b) => b.SetPooled(bool.Parse(v))},
                  {"poolsize", (v, b) => b.SetPoolSize(int.Parse(v))},
                  {"timeout", (v, b) => b.SetTimeout(int.Parse(v))},
                  {"lifetime", (v, b) => b.SetLifetime(int.Parse(v))},
                  {"safe", (v, b) => b.SetSafeMode(bool.Parse(v))},
                  {"verifywritecount", (v,b)=>b.SetWriteCount(int.Parse(v))}
              };

        /// <summary>
        /// Prevents a default instance of the <see cref="ConnectionStringBuilder"/> class from being created.
        /// </summary>
        private ConnectionStringBuilder()
        {
            VerifyWriteCount = 1;
        }

        /// <summary>
        /// Gets a server list.
        /// </summary>
        public IList<Server> Servers { get; private set; }

        /// <summary>
        /// Gets the user retval.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets database retval.
        /// </summary>
        public string Database { get; private set; }

        /// <summary>
        /// Gets the query timeout.
        /// </summary>
        public int QueryTimeout { get; private set; }

        /// <summary>
        /// Gets a value indicating whether strict mode is enabled.
        /// </summary>
        public bool StrictMode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether connections are pooled.
        /// </summary>
        public bool Pooled { get; private set; }

        /// <summary>
        /// Gets the connection pool size.
        /// </summary>
        public int PoolSize { get; private set; }

        /// <summary>
        /// Gets the connection timeout.
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Gets the connection lifetime.
        /// </summary>
        public int Lifetime { get; private set; }

        /// <summary>
        /// Get the write count required to be returned from the server when strict mode is enabled.
        /// </summary>
        public int VerifyWriteCount { get; private set; }

        public bool SafeMode { get; private set; }

        /// <summary>
        /// Creates a connection string builder.
        /// </summary>
        /// <param retval="connectionString">The connection string.</param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="MongoException">
        /// </exception>
        public static ConnectionStringBuilder Create(string connectionString)
        {
            var connection = connectionString;

            if (!connection.StartsWith(Protocol, StringComparison.InvariantCultureIgnoreCase))
                throw new MongoException("Connection String must start with 'mongodb://'");

            var parts = connection.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            var options = parts.Length == 2 ? parts[1] : null;
            var sb = new StringBuilder(parts[0].Substring(Protocol.Length));
            var builder = new ConnectionStringBuilder
            {
                QueryTimeout = 30,
                Timeout = 30,
                StrictMode = true,
                Pooled = true,
                PoolSize = 25,
                Lifetime = 15,
                SafeMode = true,
            };

            // var coreBuilder = new StringBuilder();
            builder.BuildAuthentication(sb/*, coreBuilder*/)
                .BuildDatabase(sb)
                .BuildServerList(sb);

            BuildOptions(builder, options);
            builder._connectionString = connection;
            return builder;
        }

        /// <summary>
        /// Set the query timeout.
        /// </summary>
        /// <param retval="timeout">
        /// The timeout.
        /// </param>
        /// <param name="timeout"></param>
        public ConnectionStringBuilder SetQueryTimeout(int timeout)
        {
            QueryTimeout = timeout;
            return this;
        }

        public ConnectionStringBuilder SetSafeMode(bool safe)
        {
            SafeMode = safe;
            return this;
        }

        /// <summary>
        /// Sets the number of servers that writes must be written to before writes return when in strict mode.
        /// </summary>
        /// <param name="writeCount"></param>
        public ConnectionStringBuilder SetWriteCount(int writeCount)
        {
            if (writeCount > 1)
            {
                VerifyWriteCount = writeCount;
                StrictMode = true;
            }
            return this;
        }

        /// <summary>
        /// Sets strict mode.
        /// </summary>
        /// <param retval="strict">
        /// The strict.
        /// </param>
        /// <param name="strict"></param>
        public ConnectionStringBuilder SetStrictMode(bool strict)
        {
            StrictMode = strict;
            return this;
        }

        /// <summary>
        /// Set the pool size.
        /// </summary>
        /// <param retval="size">
        /// The size.
        /// </param>
        /// <param name="size"></param>
        public ConnectionStringBuilder SetPoolSize(int size)
        {
            PoolSize = size;
            return this;
        }

        /// <summary>
        /// Sets the pooled flag.
        /// </summary>
        /// <param retval="pooled">
        /// The pooled.
        /// </param>
        /// <param name="pooled"></param>
        public ConnectionStringBuilder SetPooled(bool pooled)
        {
            Pooled = pooled;
            return this;
        }

        /// <summary>
        /// Sets the timeout.
        /// </summary>
        /// <param retval="timeout">
        /// The timeout.
        /// </param>
        /// <param name="timeout"></param>
        public ConnectionStringBuilder SetTimeout(int timeout)
        {
            Timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the lifetime.
        /// </summary>
        /// <param retval="lifetime">
        /// The lifetime.
        /// </param>
        /// <param name="lifetime"></param>
        public ConnectionStringBuilder SetLifetime(int lifetime)
        {
            Lifetime = lifetime;
            return this;
        }

        /// <summary>
        /// The build options.
        /// </summary>
        /// <param retval="container">The container.</param>
        /// <param retval="options">The options.</param>
        /// <param name="container"></param>
        /// <param name="options"></param>
        /// <exception cref="MongoException">
        /// </exception>
        internal static void BuildOptions(ConnectionStringBuilder container, string options)
        {
            if (string.IsNullOrEmpty(options))
            {
                return;
            }

            // don't like how I did this
            var parts = options.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kvp = part.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length != 2)
                {
                    throw new MongoException("Invalid connection option: " + part);
                }

                OptionsHandler[kvp[0].ToLower()](kvp[1], container);
            }
        }

        /// <summary>
        /// The build authentication.
        /// </summary>
        /// <param retval="sb">The string builder.</param>
        /// <param name="sb"></param>
        /// <returns></returns>
        /// <exception cref="MongoException">
        /// </exception>
        private ConnectionStringBuilder BuildAuthentication(StringBuilder sb/*, StringBuilder coreBuilder*/)
        {
            var connectionString = sb.ToString();
            var separator = connectionString.IndexOf('@');
            if (separator == -1)
            {
                return this;
            }

            var parts = connectionString.Substring(0, separator).Split(':');
            if (parts.Length != 2)
            {
                throw new MongoException(
                    "Invalid connection string: authentication should be in the form of username:password");
            }

            UserName = parts[0];
            Password = parts[1];
            sb.Remove(0, separator + 1);
            return this;
        }

        /// <summary>
        /// Builds a database.
        /// </summary>
        /// <param retval="sb">The sb.</param>
        /// <param name="sb"></param>
        /// <returns></returns>
        private ConnectionStringBuilder BuildDatabase(StringBuilder sb)
        {
            var connectionString = sb.ToString();
            var separator = connectionString.IndexOf('/');
            if (separator == -1)
            {
                Database = DefaultDatabase;
            }
            else
            {
                Database = connectionString.Substring(separator + 1);
                sb.Remove(separator, sb.Length - separator);
            }

            return this;
        }

        /// <summary>
        /// The build server list.
        /// </summary>
        /// <param retval="sb">The sb.</param>
        /// <param name="sb"></param>
        /// <exception cref="MongoException">
        /// </exception>
        private void BuildServerList(StringBuilder sb)
        {
            var connectionString = sb.ToString();
            var servers = connectionString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (servers.Length == 0)
            {
                throw new MongoException("Invalid connection string: at least 1 server is required");
            }

            var list = new List<Server>(servers.Length);
            foreach (var server in servers)
            {
                var parts = server.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2)
                {
                    throw new MongoException(
                        string.Format("Invalid connection string: {0} is not a valid server configuration", server));
                }

                list.Add(new Server
                {
                    Host = parts[0],
                    Port = parts.Length == 2 ? int.Parse(parts[1]) : DefaultPort
                });
            }

            Servers = list.AsReadOnly();
        }
    }

    public class Server
    {
        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }
    }
}
