// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Windows.Foundation;

namespace ContosoCookbook
{
    class AppLicenseDataSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _licensed = false;
        private string _price;

        public AppLicenseDataSource()
        {
            if (CurrentAppSimulator.LicenseInformation.IsTrial)
            {
                CurrentAppSimulator.LicenseInformation.LicenseChanged += OnLicenseChanged;
                GetListingInformationAsync();
            }
            else
                _licensed = true;
        }

        private async void GetListingInformationAsync()
        {
            var listing = await CurrentAppSimulator.LoadListingInformationAsync();
            _price = listing.FormattedPrice;
        }

        private void OnLicenseChanged()
        {
            if (!CurrentAppSimulator.LicenseInformation.IsTrial)
            {
                _licensed = true;
                CurrentAppSimulator.LicenseInformation.LicenseChanged -= OnLicenseChanged;

                ((ContosoCookbook.App)App.Current).Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (PropertyChanged != null)
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs("IsLicensed"));
                            PropertyChanged(this, new PropertyChangedEventArgs("IsTrial"));
                            PropertyChanged(this, new PropertyChangedEventArgs("LicenseInfo"));
                        }
                    });
            }
        }

        public bool IsLicensed
        {
            get { return _licensed; }
        }

        public bool IsTrial
        {
            get { return !_licensed; }
        }

        public string LicenseInfo
        {
            get
            {
                if (!_licensed)
                    return "Trial Version";
                else
                    return ("Valid until " + CurrentAppSimulator.LicenseInformation.ExpirationDate.LocalDateTime.ToString("dddd, MMMM d, yyyy"));
            }
        }

        public string FormattedPrice
        {
            get
            {
                if (!String.IsNullOrEmpty(_price))
                    return "Upgrade to the Full Version for " + _price;
                else
                    return "Upgrade to the Full Version";
            }
        }
    }
}
