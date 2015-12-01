using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Arena.Core;
using Arena.Custom.SECC.OAuth.DataLayer;

namespace Arena.Custom.SECC.OAuth
{
    public class Scope
    {
        #region Fields
        Person mCreatedBy = null;
        Person mModifiedBy = null;
        List<Client> mClients = null;
        #endregion

        #region Properties
        public int ScopeId { get; set; }
        public string Identifier { get; set; }
        public string Description { get; set; }
        public string CreatedByUserId { get; private set; }
        public string ModifiedByUserId { get; private set; }
        public DateTime DateCreated { get; private set; }
        public DateTime DateModified { get; private set; }
        public bool Active { get; set; }

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

        public List<Client> Clients
        {
            get
            {
                if ( mClients == null )
                {
                    LoadClients();
                }

                return mClients;
            }
        }
        #endregion

        #region Constructor
        public Scope()
        {
            Init();
        }

        public Scope( int id )
        {
            Load( id );
        }

        public Scope(string identifier)
        {
            Load(identifier);
        }

        public Scope(ScopeData data)
        {
            Load(data);
        }

        #endregion

        #region Public

        public static List<Scope> LoadScopes()
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                List<Scope> scopes = context.ScopeDatas.Select( s => new Scope( s ) ).ToList();
                return scopes;
            }
        }

        public static List<Scope> GetScopesByName( string identifier )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                List<Scope> scopes = context.ScopeDatas
                    .Where( s => s.scope_identifier.ToLower() == identifier.ToLower() )
                    .Select( s => new Scope( s ) ).ToList();

                return scopes;
                    
            }
        }

        public bool Save( string userId )
        {
            try
            {
                using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
                {
                    ScopeData data;

                    if ( ScopeId > 0 )
                    {
                        data = context.ScopeDatas.FirstOrDefault( s => s.scope_id == ScopeId );

                        if ( data == null )
                        {
                            throw new ArgumentException( "Scope Id is not valid.", "ScopeId" );
                        }
                    }
                    else
                    {
                        if ( ScopeIdentifierInUse() )
                        {
                            throw new ArgumentException( "Scope Identifier is already in use.", "ScopeIdentifier" );
                        }

                        data = new ScopeData();
                        data.created_by = userId;
                        data.date_created = DateTime.Now;
                    }

                    data.scope_identifier = Identifier;
                    data.scope_description = Description;
                    data.modified_by = userId;
                    data.date_modified = DateTime.Now;
                    data.active = Active;

                    if(ScopeId <= 0)
                    {
                        context.ScopeDatas.InsertOnSubmit( data );
                    }

                    context.SubmitChanges();

                    if ( data.scope_id > 0 )
                    {
                        Load( data );
                        return true;
                    }

                    return false;
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( "An error occurred while saving Scope", ex );
            }
        }

        #endregion

        #region Private

        private void Init()
        {
            ScopeId = 0;
            Identifier = null;
            Description = null;
            CreatedByUserId = null;
            ModifiedByUserId = null;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;

            mCreatedBy = null;
            mModifiedBy = null;
            mClients = null;
        }

        private void Load( int id )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                var data = context.ScopeDatas.FirstOrDefault( s => s.scope_id == id );

                Load( data );
            }
        }

        private void Load( string identifier )
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                var data = context.ScopeDatas.FirstOrDefault( s => s.scope_identifier.ToLower() == identifier.ToLower() );
                Load( data );
            }
        }

        private void Load( ScopeData data )
        {
            Init();

            if ( data != null )
            {
                ScopeId = data.scope_id;
                Identifier = data.scope_identifier;
                Description = data.scope_description;
                CreatedByUserId = data.created_by;
                ModifiedByUserId = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
            }
        }

        private void LoadClients()
        {
            using ( OAuthDataContext context = OAuthContextHelper.GetContext() )
            {
                //List<ClientData> clientData = context.ScopeDatas
                //                  .FirstOrDefault( s => s.scope_id == ScopeId )
                //                  .ClientScopeDatas.Select( s => s.ClientData ).ToList();

                //mClients = clientData.Select( c => new Client( c ) ).ToList();

                var clientData = context.ScopeDatas
                                .Where(s => s.scope_id == ScopeId)
                                .SelectMany(s => s.ClientScopeDatas.Select(cs => cs.ClientData ) )
                                .ToList();

                if ( clientData != null )
                {
                    mClients = clientData.Select( c => new Client( c ) ).ToList();
                }

            }
        }

        private bool ScopeIdentifierInUse()
        {
            Scope s = new Scope(Identifier);

            return s.ScopeId > 0;
        }

        #endregion

    }
}
