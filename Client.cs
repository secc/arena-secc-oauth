using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arena.Core;
using Arena.Custom.SECC.OAuth.DataLayer;
namespace Arena.Custom.SECC.OAuth
{

    public class Client
    {
        #region Fields
        private Person mCreatedBy = null;
        private Person mModifiedBy = null;
        private List<Scope> mScopes = null;
        #endregion

        #region Properties
        public int ClientId { get; set; }
        public string Name { get; set; }
        public Guid ApiKey { get; private set; }
        public Guid ApiSecret { get; private set; }
        public string Callback { get; set; }
        public string CreatedByUserId { get; private set; }
        public string ModifiedByUserId { get; private set; }
        public DateTime DateCreated { get; private set; }
        public DateTime DateModified { get; private set; }
        public bool Active { get; set; }

        public List<Scope> Scopes
        {
            get
            {
                if ( mScopes == null )
                {
                   LoadScopes();
                }
                return mScopes;
            }
        }


        public Person CreatedBy
        {
            get
            {
                if ( mCreatedBy == null )
                {
                    mCreatedBy = new Person( CreatedByUserId );
                }
                return mCreatedBy;
            }
        }

        public Person ModifiedBy
        {
            get
            {
                if ( mModifiedBy == null )
                {
                    mModifiedBy = new Person( ModifiedByUserId );
                }

                return mModifiedBy;
            }
        }
        #endregion

        #region Constructors
        public Client()
        {
            Init();
        }

        public Client( int clientId )
        {
            Load( clientId );
        }

        public Client( Guid apiKey )
        {
            Load( apiKey );
        }

        public Client( ClientData cd )
        {
            Load( cd );
        }

        #endregion

        public static List<Client> LoadClients( bool includeInactive )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                var query = context.ClientDatas;

                if ( !includeInactive )
                {
                    query.Where( q => q.active );
                }

                return query.Select( q => new Client( q ) ).ToList();
            }
        }

        public void AddScope(int scopeId)
        {
            if ( Scopes.FirstOrDefault( s => s.ScopeId == scopeId ) == null )
            {
                Scope s = new Scope( scopeId );
                if ( s.ScopeId > 0 )
                {
                    Scopes.Add( s );
                }
                else
                {
                    throw new ArgumentException( "Scope not found.", "scopeId" );
                }
            }
        }

        public void RemoveScope( int scopeId )
        {
            var scopeToRemove = Scopes.FirstOrDefault( s => s.ScopeId == scopeId );

            if ( scopeToRemove != null )
            {
                Scopes.Remove( scopeToRemove );
            }
        }

        public void GenerateApiKey()
        {
            ApiKey = Guid.NewGuid();
        }

        public void  GenerateApiSecret()
        {
            ApiSecret = Guid.NewGuid();
        }



        public bool Save(string userId)
        {
            try
            {
                using (OAuthDataContext context = OAuthContextHelper.GetContext())
                {
                    ClientData data;

                    if ( ClientId > 0 )
                    {
                        data = context.ClientDatas.FirstOrDefault( c => c.client_id == ClientId );
                        if ( data == null )
                        {
                            throw new ArgumentOutOfRangeException( "Client Not Found for ClientID" );
                        }
                    }
                    else
                    {
                        data = new ClientData();

                        if ( ApiKey == Guid.Empty )
                        {
                            GenerateApiKey();
                        }
                        if ( ApiSecret == Guid.Empty )
                        {
                            GenerateApiSecret();
                        }
                    }

                    data.client_name = Name;
                    data.callback_url = Callback;
                    data.api_key = ApiKey;
                    data.api_secret = ApiSecret;
                    data.modified_by = userId;
                    data.date_modified = DateTime.Now;
                    data.active = Active;

                    if ( data.client_id <= 0 )
                    {
                        data.created_by = userId;
                        data.date_created = DateTime.Now;

                        if ( Scopes != null )
                        {
                            foreach ( var scope in Scopes )
                            {
                                data.ClientScopeDatas.Add( new ClientScopeData
                                {
                                    scope_id = scope.ScopeId,
                                    created_by = userId,
                                    date_created = DateTime.Now,
                                    modified_by = userId,
                                    date_modified = DateTime.Now,
                                    active = true
                                } );
                            }
                        }


                        context.ClientDatas.InsertOnSubmit( data );
                    }
                    else
                    {
                        var scopeIDRemove = data.ClientScopeDatas.Select( cs => cs.scope_id ).Where( cs => !Scopes.Select( s => s.ScopeId ).Contains( cs ) ).ToArray() ;
                        foreach ( var scopeId in scopeIDRemove )
                        {
                            var scopeToRemove = data.ClientScopeDatas.Where( cs => cs.scope_id == scopeId ).FirstOrDefault();

                            if ( scopeToRemove != null )
                            {
                                context.ClientScopeDatas.DeleteOnSubmit( scopeToRemove );
                            }
                        }

                        var scopeIdAdd = Scopes.Select( s => s.ScopeId ).Where( s => !data.ClientScopeDatas.Select( cs => cs.scope_id ).Contains( s ) );


                        foreach ( var scopeId in scopeIdAdd )
                        {
                            data.ClientScopeDatas.Add(
                                    new ClientScopeData
                                    {
                                        scope_id = scopeId,
                                        created_by = userId,
                                        modified_by = userId,
                                        date_created = DateTime.Now,
                                        date_modified = DateTime.Now,
                                        active = true
                                    }
                                );
                        }
                    }

                    context.SubmitChanges();

                    if ( data.client_id > 0 )
                    {
                        Load( data );
                        return true;
                    }

                    return false;
                }
            }
            catch ( Exception ex )
            {

                throw new Exception( "An error occurred when saving client.", ex );
            }

        }

        #region Private
        private void Init()
        {
            ClientId = 0;
            Name = null;
            ApiKey = Guid.Empty;
            ApiSecret = Guid.Empty;
            Callback = null;
            CreatedByUserId = null;
            ModifiedByUserId = null;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
            mCreatedBy = null;
            mModifiedBy = null;
            mScopes = null;

        }



        private void Load( ClientData data )
        {
            Init();

            if ( data == null )
            {
                return;
            }

            ClientId = data.client_id;
            Name = data.client_name;
            ApiKey = data.api_key;
            ApiSecret = data.api_secret;
            Callback = data.callback_url;
            CreatedByUserId = data.created_by;
            ModifiedByUserId = data.modified_by;
            Active = data.active;
        }

        private void Load( int clientId )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext())
            {
                var client = context.ClientDatas.FirstOrDefault( c => c.client_id == clientId );

                Load( client );
            }   
        }

        private void Load( Guid apiKey )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                var client = context.ClientDatas.FirstOrDefault( c => c.api_key == apiKey );

                Load( client );
            }
        }

        private void LoadScopes()
        {
            mScopes = null;
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                var scopeDatas = context.ClientDatas
                                    .Where( c => c.client_id == ClientId )
                                    .SelectMany( c => c.ClientScopeDatas.Select( cs => cs.ScopeData ) )
                                    .ToList();

                if(scopeDatas != null)
                {
                    mScopes = scopeDatas.Select( s => new Scope( s ) ).ToList();
                }
                
            }
        }
        #endregion
    }
}
