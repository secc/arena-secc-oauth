using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arena.Core;
using Arena.Custom.SECC.OAuth.DataLayer;

namespace Arena.Custom.SECC.OAuth
{
    public class Authorization
    {
        #region Fields
        Person mUser = null;
        Client mClient = null;
        Scope mScope = null;
        #endregion

        #region Properties
        public int AuthorizationId { get; set; }
        public int ClientId { get; set; }
        public int ScopeId { get; set; }
        public string LoginId { get; set;  }
        public DateTime DateCreated { get; private set; }
        public bool Active { get; set; }

        public Person User
        {
            get
            {
                if ( mUser == null )
                {
                    mUser = new Person( LoginId );
                }

                return mUser;
            }
        }

        public Client Client
        {
            get
            {
                if ( ( mClient == null && ClientId > 0 ) || ( mClient != null && mClient.ClientId != ClientId ) )
                {
                    mClient = new Client( ClientId );
                }

                return mClient;
            }
        }

        public Scope Scope
        {
            get
            {
                if ( ( mScope == null && ScopeId > 0 ) || ( mScope != null && mScope.ScopeId != ScopeId ) )
                {
                    mScope = new Scope( ScopeId );
                }

                return mScope;
            }
        }

        #endregion

        #region Constructors
        public Authorization()
        {
            Init();
        }

        public Authorization( int authId )
        {
            Load( authId );
        }

        public Authorization( AuthorizationData data )
        {
            Load( data );
        }

        #endregion

        #region Public

        public static List<ClientAuthorization> GetUserAuthorizationsForClient( string loginId, string clientKey, bool includeInactive )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                Guid clientKeyGuid = Guid.Empty;

                Guid.TryParse(clientKey, out clientKeyGuid);
                var query = context.AuthorizationDatas
                    .Where( a => a.login_id == loginId )
                    .Where( a => a.ClientData.api_key == clientKeyGuid );

                if ( !includeInactive )
                {
                    query = query.Where( a => a.active );
                }

                var clientAuth = query.Select( a => new ClientAuthorization()
                     {
                         AuthorizationId = a.authorization_id,
                         ClientId = a.client_id,
                         ScopeId = a.scope_id,
                         LoginId = a.login_id,
                         ScopeIdentifier = a.ScopeData.scope_identifier,
                         ScopeDescription = a.ScopeData.scope_description,
                         Active = a.active
                     } ).ToList();

                return clientAuth;
            }
        }

        public static void AuthorizeScopes( string loginId,  string clientKey, string[] scopes )
        {
            Guid clientKeyGuid;

            if ( !Guid.TryParse( clientKey, out clientKeyGuid ) )
            {
                throw new ArgumentException( "Client Key/ID is not valid", "clientKey" );
            }

            Client c = new Client( clientKeyGuid );

            if ( c.ClientId <= 0 )
            {
                throw new ArgumentException( "Client Key/ID is not valid.", "clientKey" );
            }

            foreach ( var scope in scopes )
            {
                using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
                {
                    bool isExistingAuth = context.AuthorizationDatas
                            .Where( a => a.client_id == c.ClientId )
                            .Where( a => a.login_id.ToLower() == loginId.ToLower())
                            .Where( a => a.ScopeData.scope_identifier.ToLower() == scope.ToLower())
                            .Where(a => a.active)
                            .Count() > 0;

                    bool isClientScope = c.Scopes.Where( s => s.Identifier.ToLower() == scope.ToLower())
                                            .Where( s => s.Active ).Count() > 0;

                    if ( isClientScope && !isExistingAuth )
                    {
                        AddUserAuthorization( loginId, c.ClientId, new Scope(scope).ScopeId );
                    }
                    else if ( !isClientScope )
                    {
                        throw new ArgumentException( string.Format( "Scope {0} is not valid for client.", scope ), "scope" );
                    }
                }
            }
        }

        public bool Save()
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                AuthorizationData data;

                if ( AuthorizationId > 0 )
                {
                    data = context.AuthorizationDatas.FirstOrDefault( a => a.authorization_id == AuthorizationId );
                }
                else
                {
                    data = new AuthorizationData();
                    data.date_created = DateTime.Now;
                }

                data.client_id = ClientId;
                data.login_id = LoginId;
                data.scope_id = ScopeId;
                data.active = Active;

                if ( AuthorizationId == 0 )
                {
                    context.AuthorizationDatas.InsertOnSubmit( data );
                }

                context.SubmitChanges();

                Load( data );

                return AuthorizationId > 0;
            }
        }

        /// <summary>
        /// Check to see if the user is allowed to perform the operation
        /// </summary>
        /// <param name="operationType">Arena OperationType</param>
        /// <param name="currentUser">The current user/principal</param>
        /// <returns></returns>
        public bool Allowed(Security.OperationType operationType, 
            System.Security.Principal.GenericPrincipal currentUser)
        {
            if (operationType.Equals(Security.OperationType.View))
            {

                // The user can only view authorizations for themselves
                if (currentUser.Identity == this.User)
                {
                    return true;
                }
            }
            if (operationType.Equals(Security.OperationType.Edit))
            {
                // The user can only edit authorizations for themselves
                if (currentUser.Identity.Name != this.LoginId)
                {
                    return false;
                }

                // Make sure the client has the scope
                foreach(Scope scope in this.Client.Scopes)
                {
                    if (scope.ScopeId == this.ScopeId)
                    {
                        return true;
                    }
                }
                
            }
            return false;
        }

        #endregion

        #region Private

        private static bool AddUserAuthorization( string loginId, int clientId, int scopeID )
        {
            Authorization a = new Authorization();

            a.LoginId = loginId;
            a.ClientId = clientId;
            a.ScopeId = scopeID;

            return a.Save();
        }

        private void Init()
        {
            AuthorizationId = 0;
            ClientId = 0;
            ScopeId = 0;

            LoginId = null;
            DateCreated = DateTime.MinValue;
            Active = false;

            mClient = null;
            mScope = null;
            mUser = null;
        }

        private void Load( int authId )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                var auth = context.AuthorizationDatas.FirstOrDefault( a => a.authorization_id == authId );

                Load( auth );
            }
        }

        private void Load( AuthorizationData data )
        {
            Init();

            if ( data != null )
            {
                AuthorizationId = data.authorization_id;
                ClientId = data.client_id;
                ScopeId = data.scope_id;
                LoginId = data.login_id;
                DateCreated = data.date_created;
                Active = data.active;
            }
        }

        #endregion
    }

    public class ClientAuthorization
    {
        public int AuthorizationId { get; set; }
        public int ClientId { get; set; }
        public int ScopeId { get; set; }
        public string LoginId { get; set; }
        public string ScopeIdentifier { get; set; }
        public string ScopeDescription { get; set; }
        public bool Active { get; set; }

    }
}
