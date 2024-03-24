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

namespace FSV.ViewModel.Permission
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Abstractions;
    using AdMembers;
    using Configuration.Sections.ConfigXml;
    using Core;
    using FileSystem.Interop;
    using FileSystem.Interop.Abstractions;
    using Microsoft.Extensions.Logging;
    using Resources;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class PermissionItemAclViewModel : PermissionItemBase
    {
        private readonly IAclModelBuilder _aclModelBuilder;
        private readonly Func<ReportTrustee> _configReportTrustee;
        private readonly IAclViewProvider accessControlListViewProvider;

        private readonly IFlyOutService flyOutService;
        private readonly ILogger<PermissionItemAclViewModel> logger;
        private IEnumerable<IAclModel> _aclList;
        private IAclModel _selectedItem;

        internal PermissionItemAclViewModel(
            IFlyOutService flyOutService,
            IAclModelBuilder aclModelBuilder,
            IAclViewProvider accessControlListViewProvider,
            ILogger<PermissionItemAclViewModel> logger,
            Func<ReportTrustee> configReportTrustee,
            string folderPath) : base(folderPath)
        {
            this.flyOutService = flyOutService ?? throw new ArgumentNullException(nameof(flyOutService));
            this._aclModelBuilder = aclModelBuilder ?? throw new ArgumentNullException(nameof(aclModelBuilder));
            this.accessControlListViewProvider = accessControlListViewProvider ?? throw new ArgumentNullException(nameof(accessControlListViewProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._configReportTrustee = configReportTrustee ?? throw new ArgumentNullException(nameof(configReportTrustee));

            this.Icon = "ACLIcon";
            this.DisplayName = PermissionResource.ACLCaption;
            this.CanResize = true;

            this.ShowGroupMembersCommand = new RelayCommand(this.ShowGroupMembers, this.CanShowGroupMembers);

            this.LoadAclAsync().FireAndForgetSafeAsync();
        }

        public ICommand ShowGroupMembersCommand { get; }

        public IAclModel SelectedItem
        {
            get => this._selectedItem;
            set => this.Set(ref this._selectedItem, value, nameof(this.SelectedItem));
        }

        public IEnumerable<IAclModel> AccessControlList
        {
            get => this._aclList;
            private set => this.Set(ref this._aclList, value, nameof(this.AccessControlList));
        }

        private string GetParentPath(string path)
        {
            return path.Substring(0, path.LastIndexOf(@"\"));
        }

        private void LogAcl(IEnumerable<IAclModel> aclData)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("ACL of {0}\r\n", this.FolderPath);
            builder.AppendLine("ACL as [AccountName] [Type] [Rights] [Inherited] [Flags]");
            // Writing ACL in Log File.
            foreach (IAclModel item in aclData)
            {
                builder.AppendFormat("[{0}]\t[{1}]\t[{2}]\t[{3}]\t[{4}]\r\n", item.Account, item.TypeString, item.RightsString, item.Inherited, item.InheritanceFlagsString);
            }

            this.logger.LogInformation(builder.ToString());
        }

        private void ShowGroupMembers(object _)
        {
            this.flyOutService.Show<GroupMembersViewModel>(this.SelectedItem.Account);
        }

        private bool CanShowGroupMembers(object _)
        {
            return this.SelectedItem != null && this.SelectedItem.AccountTypeString == AccountType.Group.ToString();
        }

        private async Task LoadAclAsync()
        {
            await Task.Run(() =>
            {
                IEnumerable<IAcl> folderAclView = this.accessControlListViewProvider.GetAclView(this.FolderPath);
                IEnumerable<IAclModel> folderAclViewModels = folderAclView.Select(this._aclModelBuilder.Build).ToList();

                string parentPath = this.GetParentPath(this.FolderPath);
                if (parentPath != null && parentPath.StartsWith(@"\\") && parentPath.EndsWith(@"\"))
                {
                    IEnumerable<IAcl> parentAcls = this.accessControlListViewProvider.GetAclView(parentPath);
                    IEnumerable<IAclModel> rootAcl = parentAcls.Select(this._aclModelBuilder.Build).ToList();

                    bool areEqual = rootAcl.IsAclEqual(folderAclViewModels);
                }

                ReportTrustee reportTrustee = this._configReportTrustee();
                if (reportTrustee.Settings.ShowAcl)
                {
                    this.AccessControlList = folderAclViewModels;
                }

                this.LogAcl(folderAclViewModels);
            });
        }
    }
}