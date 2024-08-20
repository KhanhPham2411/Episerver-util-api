// Source: https://world.optimizely.com/blogs/Daniel-Ovaska/Dates/2019/6/content-events-in-episerver/

// Initialization module to hook up all events and setup a new event type for ContentChanged
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class ChangeEventInitialization : IInitializableModule
    {
        private ILogger _log = LogManager.GetLogger(typeof(ChangeEventInitialization));
        
        public void Initialize(InitializationEngine context)
        {
            var events = ServiceLocator.Current.GetInstance<IContentEvents>();
            var contentSecurityRepo = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            contentSecurityRepo.ContentSecuritySaved += ContentSecurityRepo_ContentSecuritySaved;
            events.MovedContent += Events_MovedContent;
            events.PublishingContent += Events_PublishingContent;
            events.PublishedContent += Events_PublishedContent;
            events.DeletedContent += Events_DeletedContent;
            ExtendedContentEvents.Instance.ContentChanged += Instance_ContentChanged;
        }

        private void Instance_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            _log.Information($"Events ContentChanged fired for content {JsonConvert.SerializeObject(e)}");
        }

        private void Events_PublishingContent(object sender, EPiServer.ContentEventArgs e)
        {
            _log.Information($"Events_PublishingContent fired for content {e.Content.ContentLink.ID}");
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            var oldUrl = urlResolver.GetUrl(new ContentReference(e.Content.ContentLink.ID));
            e.Items.Add("Url", oldUrl);
            _log.Information($"Old url: {oldUrl}");
        }

        private void ContentSecurityRepo_ContentSecuritySaved(object sender, ContentSecurityEventArg e)
        {
            _log.Information($"ContentSecuritySaved fired for content {e.ContentLink.ID}");
            var action = ContentAction.AccessRightsChanged;
            var affectedContent = new List<ContentReference>();
            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            var descendants = contentRepository.GetDescendents(e.ContentLink);
            affectedContent.AddRange(descendants);
            affectedContent.Add(e.ContentLink);
            ExtendedContentEvents.Instance.RaiseContentChangedEvent(new ContentChangedEventArgs(e.ContentLink, action, affectedContent));
        }
        

        private void Events_DeletedContent(object sender, EPiServer.DeleteContentEventArgs e)
        {
            _log.Information($"Deleted content fired for content {e.ContentLink.ID}");
            var eventArgs = e as DeleteContentEventArgs;
            if(eventArgs!=null)
            {
                var action = ContentAction.ContentDeleted;
                var affectedContent = new List<ContentReference>();
                affectedContent.AddRange(eventArgs.DeletedDescendents);
                if(e.ContentLink.ID!=ContentReference.WasteBasket.ID)
                {
                    affectedContent.Add(e.ContentLink);
                }
                ExtendedContentEvents.Instance.RaiseContentChangedEvent(new ContentChangedEventArgs(e.ContentLink, action, affectedContent));
            }
        }

        private void Events_MovedContent(object sender, EPiServer.ContentEventArgs e)
        {
            _log.Information($"Moved content fired for content {e.ContentLink.ID}");
            var eventargs = e as MoveContentEventArgs;
            if(eventargs!=null)
            {
                var action = ContentAction.ContentMoved;
                if(eventargs.TargetLink.ID == ContentReference.WasteBasket.ID)
                {
                    action = ContentAction.ContentMovedToWastebasket;
                }
                if(eventargs.OriginalParent.ID==ContentReference.WasteBasket.ID)
                {
                    action = ContentAction.ContentMovedFromWastebasket;
                }
                var affectedContent = new List<ContentReference>();
                affectedContent.AddRange(eventargs.Descendents);
                affectedContent.Add(e.ContentLink);
                ExtendedContentEvents.Instance.RaiseContentChangedEvent(new ContentChangedEventArgs(e.ContentLink, action, affectedContent));
            }
           
        }

        private void Events_PublishedContent(object sender, EPiServer.ContentEventArgs e)
        {
            _log.Information($"Published content fired for content {e.ContentLink.ID}");
            var urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            var url = urlResolver.GetUrl(e.ContentLink);
            _log.Information($"New url: {url}");
            if (e.Items["Url"]!=null)
            {
                var oldUrl = e.Items["Url"].ToString();
                if(url!=oldUrl)
                {
                    _log.Information($"Url changed for {e.ContentLink.ID}");
                    var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
                    var descendants = contentRepository.GetDescendents(e.ContentLink);
                    var affectedContent = new List<ContentReference>();
                    affectedContent.AddRange(descendants);
                    affectedContent.Add(new ContentReference(e.ContentLink.ID));
                    ExtendedContentEvents.Instance.RaiseContentChangedEvent(new ContentChangedEventArgs(e.ContentLink, ContentAction.UrlChanged, affectedContent));
                }
                else
                {
                    ExtendedContentEvents.Instance.RaiseContentChangedEvent(new ContentChangedEventArgs(e.ContentLink, ContentAction.ContentPublished, new List<ContentReference>()));
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            var events = ServiceLocator.Current.GetInstance<IContentEvents>();
            var contentSecurityRepo = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            contentSecurityRepo.ContentSecuritySaved -= ContentSecurityRepo_ContentSecuritySaved;
            events.MovedContent -= Events_MovedContent;
            events.PublishingContent -= Events_PublishingContent;
            events.PublishedContent -= Events_PublishedContent;
            events.DeletedContent -= Events_DeletedContent;
            ExtendedContentEvents.Instance.ContentChanged -= Instance_ContentChanged;
        }

        public void Preload(string[] parameters)
        {

        }
        
    }
    //New event args class that can store a list of descendents that were affected
    //and the type of source event
    public class ContentChangedEventArgs : EventArgs
    {
        
        public ContentReference SourceContentLink { get; }
        public ContentAction Action { get; }
        public ContentChangedEventArgs(ContentReference sourceContentLink, ContentAction action, IEnumerable<ContentReference> affectedContent)
        {
            SourceContentLink = sourceContentLink;
            Action = action;
            AffectedContent = affectedContent;
        }
        /// <summary>
        /// Includes references to all affected content including the content that triggered the event
        /// </summary>
        public IEnumerable<ContentReference> AffectedContent { get; }
    }
    //New enum to specify the original action that changed the content. 
    //Can be extended if needed to include the entire source event
    public enum ContentAction
    {
        ContentPublished,
        ContentDeleted,
        ContentMoved,
        AccessRightsChanged,
        UrlChanged,
        ContentMovedToWastebasket,
        ContentMovedFromWastebasket
    }
   //Some infrastructure to make it possible to listen on the changeevent, 
   ///raise a new event etc.
   public class ExtendedContentEvents
    {
        public const string CreatingLanguageEventKey = "ContentChangedEvent";
        private EventHandlerList Events
        {
            get
            {
                if (_events == null)
                    throw new ObjectDisposedException(this.GetType().FullName);
                return _events;
            }
        }
        private EventHandlerList _events = new EventHandlerList();
        private static object _keyLock = new object();
        private static ExtendedContentEvents _instance;
        internal const string ChangedEvent = "ChangedEvent";
        public static ExtendedContentEvents Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_keyLock)
                    {
                        if (_instance == null)
                            _instance = new ExtendedContentEvents();
                    }
                }
                return _instance;
            }
        }
        private object GetEventKey(string stringKey)
        {
            object obj;
            if (!_eventKeys.TryGetValue(stringKey, out obj))
            {
                lock (_keyLock)
                {
                    if (!this._eventKeys.TryGetValue(stringKey, out obj))
                    {
                        obj = new object();
                        _eventKeys[stringKey] = obj;
                    }
                }
            }
            return obj;
        }
        private Dictionary<string, object> _eventKeys = new Dictionary<string, object>();
        public event EventHandler<ContentChangedEventArgs> ContentChanged
        {
            add
            {
                Events.AddHandler(this.GetEventKey("ContentChangedEvent"), (Delegate)value);
            }
            remove
            {
                Events.RemoveHandler(this.GetEventKey("ContentChangedEvent"), (Delegate)value);
            }
        }
        public virtual void RaiseContentChangedEvent(ContentChangedEventArgs eventArgs)
        {
            var eventHandler = Events[GetEventKey(CreatingLanguageEventKey)] as EventHandler<ContentChangedEventArgs>;
            if (eventHandler != null)
            {
                eventHandler((object)this, eventArgs);
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            if (_events != null)
            {
                _events.Dispose();
                _events = (EventHandlerList)null;
            }
            if (this != _instance)
                return;
            _instance = null;
        }
    }
