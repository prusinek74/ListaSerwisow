using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ListaSerwisow.ViewModels
{
    public class ServiceLocator
    {
        private ArcGIS4Server server = null;
        public ArcGIS4Server Server
        {
            get
            {
                if (server == null)
                    server = new ArcGIS4Server();
                return server;
            }
        }
    }

    public abstract class ViewModelBase<T> : INotifyPropertyChanged where T:class
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected   void NotifyPropertyChanged(System.Linq.Expressions.Expression<Func<T,object>> arg)
        {
            string propName = string.Empty;
            if (arg.Body is System.Linq.Expressions.MemberExpression)
                propName = (arg.Body as System.Linq.Expressions.MemberExpression).Member.Name;
            else
                if (arg.Body is System.Linq.Expressions.UnaryExpression)
                    propName = ((arg.Body as System.Linq.Expressions.UnaryExpression).Operand as System.Linq.Expressions.MemberExpression).Member.Name;

            var h = PropertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class ArcGIS4Server:ViewModelBase<ArcGIS4Server>
    {
            private string rootUrl;
            public string RootUrl
            {
                get { return rootUrl; }
                set
                {
                    rootUrl = value;
                    NotifyPropertyChanged(m => m.RootUrl);
                }
            }

            public bool tokenRequired;
            public bool TokenRequired
            {
                get { return tokenRequired; }
                set
                {
                    tokenRequired = value;
                    NotifyPropertyChanged(m => m.TokenRequired);

                }
            }

            private ESRI.ArcGIS.Client.IdentityManager.Credential currentCredential;
            public ESRI.ArcGIS.Client.IdentityManager.Credential CurrentCredential
            {
                get { return currentCredential; }
                set
                {
                    currentCredential = value;
                    NotifyPropertyChanged(m => m.CurrentCredential);
                }
            }

            private ObservableCollection<ListaSerwisow.ArcGIS.ServiceDescription> serwisy;
            public ObservableCollection<ListaSerwisow.ArcGIS.ServiceDescription> Serwisy
            {
                get { return serwisy; }
                set
                {
                    serwisy = value;
                    NotifyPropertyChanged(m => m.Serwisy);
                }
            }
        
            public ArcGIS4Server()
            {
                ESRI.ArcGIS.Client.IdentityManager.Current.ChallengeMethod = ESRI.ArcGIS.Client.Toolkit.SignInDialog.DoSignIn;
    //            SetupCli();
                this.Serwisy = new ObservableCollection<ArcGIS.ServiceDescription>();
                this.PropertyChanged += ArcGIS4Server_PropertyChanged;
                var cli = new ArcGIS.ServiceCatalogPortClient();
                this.RootUrl = cli.Endpoint.Address.Uri.ToString();

            }

            private ArcGIS.ServiceCatalogPortClient SetupCli(string url)
            {
                var cli = new ArcGIS.ServiceCatalogPortClient();
                (cli.Endpoint.Binding as System.ServiceModel.BasicHttpBinding).EnableHttpCookieContainer = true;
                //(cli.Endpoint.Binding as System.ServiceModel.BasicHttpBinding).CreateBindingElements();
                UriBuilder epa = new UriBuilder(url);

                //To nie działa - token jest przekazany w adresie ale efektu nie ma

                if (CurrentCredential != null)
                    epa.Query = string.Format("token={0}", CurrentCredential.Token);

                cli.ChannelFactory.Endpoint.Address = new System.ServiceModel.EndpointAddress( epa.ToString());
                cli.Endpoint.Address = new System.ServiceModel.EndpointAddress(epa.ToString());

                //To nie działa - nie przekazuje ciasteczka - dodatkowo występuje problem z utworzeniem cookie gdy adres jest bez domenowy (np http://geosrvoracle/arcgis/services) - nie da się ustawić domeny ciasteczka na geosrvoracle


                //if (cli.CookieContainer == null)
                //    cli.CookieContainer = new CookieContainer();
                //if (CurrentCredential != null)
                //    cli.CookieContainer.Add(epa.Uri, new Cookie { Domain = epa.Uri.DnsSafeHost });



                cli.GetServiceDescriptionsCompleted += cli_GetServiceDescriptionsCompleted;
                cli.RequiresTokensCompleted += cli_RequiresTokensCompleted;
                return cli;


            }

 
            void cli_RequiresTokensCompleted(object sender, ArcGIS.RequiresTokensCompletedEventArgs e)
            {
                if (e.Error == null)
                    if (e.Result)
                    {

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                if (e.Error == null)
                                    this.TokenRequired = e.Result;
                            });
                    }
            }
            void ArcGIS4Server_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case "RootUrl":
                        SetupCli(this.RootUrl).RequiresTokensAsync();
                        break;
                    case "TokenRequired":
                        if (this.TokenRequired)
                            ESRI.ArcGIS.Client.IdentityManager.Current.GetCredentialAsync(this.RootUrl.Replace("/services", "/rest/services"), false, (c, er) =>
                                {
                                    Deployment.Current.Dispatcher.BeginInvoke(() => this.CurrentCredential = c);

                                });
                        else
                            this.CurrentCredential = null;

                        break;
                    case "CurrentCredential":
                        SetupCli(this.RootUrl).GetServiceDescriptionsAsync();

                        break;

                    default:
                        break;
                }
            }

            void cli_GetServiceDescriptionsCompleted(object sender, ArcGIS.GetServiceDescriptionsCompletedEventArgs e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.Serwisy.Clear();
                    if (e.Error == null)
                    foreach (var item in e.Result)
                    {
                        this.Serwisy.Add(item);
                    }
                });
            }

    }
}
