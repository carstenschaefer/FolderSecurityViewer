// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.ViewModel.UserReport
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Core;
    using Database.Models;
    using Resources;

    public class SavedFolderItemViewModel
    {
        private static readonly IDictionary<string, string> OrderedPropertyMap = new Dictionary<string, string>(5)
        {
            { nameof(CompleteName), UserReportResource.SavedReportNameCaption },
            { nameof(SubFolder), UserReportResource.SavedReportSubFolderCaption },
            { nameof(Domain), UserReportResource.SavedReportDomainCaption },
            { nameof(OriginatingGroup), UserReportResource.SavedReportOriginatingGroupCaption },
            { nameof(Permissions), UserReportResource.SavedReportPermissionsCaption }
        };

        private readonly UserPermissionReportDetail _permissionReportDetail;

        public SavedFolderItemViewModel(UserPermissionReportDetail permissionReportDetail, bool encrypted)
        {
            this._permissionReportDetail = permissionReportDetail ?? throw new ArgumentNullException(nameof(permissionReportDetail));

            this.SetValues(encrypted);
        }

        public int Id { get; set; }

        public string CompleteName { get; private set; }

        public string Domain { get; private set; }

        public string OriginatingGroup { get; private set; }

        public string Permissions { get; set; }

        public string SubFolder { get; set; }

        public static IReadOnlyDictionary<string, string> DisplayColumns { get; } = new ReadOnlyDictionary<string, string>(OrderedPropertyMap);

        private void SetValues(bool encrypted)
        {
            this.Id = this._permissionReportDetail.Id;

            if (encrypted)
            {
                this.CompleteName = this._permissionReportDetail.CompleteName?.Decrypt();
                this.Domain = this._permissionReportDetail.Domain?.Decrypt();
                this.OriginatingGroup = this._permissionReportDetail.OriginatingGroup?.Decrypt();
                this.Permissions = this._permissionReportDetail.Permissions?.Decrypt();
                this.SubFolder = this._permissionReportDetail.SubFolder?.Decrypt();
            }
            else
            {
                this.CompleteName = this._permissionReportDetail.CompleteName;
                this.Domain = this._permissionReportDetail.Domain;
                this.OriginatingGroup = this._permissionReportDetail.OriginatingGroup;
                this.Permissions = this._permissionReportDetail.Permissions;
                this.SubFolder = this._permissionReportDetail.SubFolder;
            }
        }

        //public static Dictionary<string, string> GetDisplayColumns()
        //{
        //    var propertyMap = new Dictionary<string, string>(5);

        //    var properties = typeof(SavedFolderItemViewModel).GetProperties()
        //      .Where(m => m.CustomAttributes.Where(n => n.AttributeType == typeof(DisplayOrderAttribute)).Any())
        //      .OrderBy(m =>
        //      {
        //          var displayOrder = m.GetCustomAttributes(typeof(DisplayOrderAttribute), true).FirstOrDefault() as DisplayOrderAttribute;
        //          return displayOrder.Order;
        //      });

        //    foreach (var property in properties)
        //    {
        //        var attribute = property.GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
        //        if (attribute != null)
        //        {
        //            propertyMap.Add(property.Name, attribute.DisplayName);
        //        }
        //    }

        //    return propertyMap;
        //}
    }
}