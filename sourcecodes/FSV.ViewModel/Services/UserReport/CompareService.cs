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

namespace FSV.ViewModel.Services.UserReport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Compare;
    using Resources;
    using ViewModel.UserReport;

    public class CompareService : ICompareService
    {
        private readonly IUserPermissionReportManager _reportManager;

        public CompareService(IUserPermissionReportManager reportManager)
        {
            this._reportManager = reportManager ?? throw new ArgumentNullException(nameof(reportManager));
        }

        public async Task<IList<CompareUserReportItemViewModel>> Compare(int reportIdOne, int reportIdTwo)
        {
            IEnumerable<UserReportDetailListItemViewModel> one = await this._reportManager.GetReportAsync(reportIdOne);
            IEnumerable<UserReportDetailListItemViewModel> two = await this._reportManager.GetReportAsync(reportIdTwo);

            IEnumerable<JoinReport> joinedReports = await this.InternalCompare(one, two);

            var comparedItems = new List<CompareUserReportItemViewModel>(joinedReports.Count());

            if (comparedItems.Count > 500)
            {
                Parallel.ForEach(joinedReports, report => comparedItems.Add(this.GetComparedItem(report)));
            }
            else
            {
                await Task.Run(() =>
                {
                    foreach (JoinReport item in joinedReports)
                    {
                        comparedItems.Add(this.GetComparedItem(item));
                    }
                });
            }

            return comparedItems;
        }

        private Task<IEnumerable<JoinReport>> InternalCompare(IEnumerable<UserReportDetailListItemViewModel> oldReportDetails, IEnumerable<UserReportDetailListItemViewModel> newReportDetails)
        {
            return Task.Run(() =>
            {
                var oldReportItems = oldReportDetails
                    .OrderBy(m => m.Detail.Id)
                    .GroupBy(m => m.CompleteName)
                    .Select(group => new { Group = group, Count = group.Count() })
                    .SelectMany(groupWithCount =>
                        groupWithCount.Group.Zip(
                            Enumerable.Range(1, groupWithCount.Count),
                            (oldReport, rowNumber) => new { Report = oldReport, RowNumber = rowNumber }
                        )
                    );

                var newReportItems = newReportDetails
                    .OrderBy(m => m.Detail.Id)
                    .GroupBy(m => m.CompleteName)
                    .Select(group => new { Group = group, Count = group.Count() })
                    .SelectMany(groupWithCount =>
                        groupWithCount.Group.Zip(
                            Enumerable.Range(1, groupWithCount.Count),
                            (newReport, rowNumber) => new { Report = newReport, RowNumber = rowNumber }
                        )
                    );

                IEnumerable<JoinReport> olderJoin = from oldReport in oldReportItems
                    join newReport in newReportItems on
                        new { oldReport.Report.CompleteName, oldReport.RowNumber } equals new { newReport?.Report.CompleteName, RowNumber = newReport?.RowNumber ?? 0 } into nrp
                    from newReport in nrp.DefaultIfEmpty()
                    select new JoinReport(oldReport.Report.CompleteName, oldReport.Report, newReport?.Report, oldReport.RowNumber);

                IEnumerable<JoinReport> newerJoin = from newReport in newReportItems
                    join oldReport in oldReportItems on
                        new { newReport.Report.CompleteName, newReport.RowNumber } equals new { oldReport?.Report.CompleteName, RowNumber = oldReport?.RowNumber ?? 0 } into orp
                    from oldReport in orp.DefaultIfEmpty()
                    select new JoinReport(newReport.Report.CompleteName, oldReport?.Report, newReport.Report, newReport.RowNumber);

                return olderJoin.Union(newerJoin, new JoinReportEqualityComparer());
            });
        }

        private CompareUserReportItemViewModel GetComparedItem(JoinReport report)
        {
            var comparedItem = new CompareUserReportItemViewModel
            {
                CompleteName = report.CompleteName,
                OldPermission = report.OldReport?.Permissions ?? string.Empty,
                NewPermission = report.NewReport?.Permissions ?? string.Empty
            };

            if (report.NewReport == null)
            {
                comparedItem.State = CompareState.Removed;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionRemovedText, report.CompleteName);
            }
            else if (report.OldReport == null)
            {
                comparedItem.State = CompareState.Added;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionAddedText, report.CompleteName, report.NewReport.Permissions);
            }
            else if (report.OldReport.Permissions.Equals(report.NewReport.Permissions))
            {
                comparedItem.State = CompareState.Similar;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionUnchangedText, report.CompleteName, report.NewReport.Permissions);
            }
            else
            {
                comparedItem.State = CompareState.Changed;
                comparedItem.Text = string.Format(PermissionCompareResource.PermissionChangedText, report.CompleteName, report.NewReport.Permissions);
            }

            return comparedItem;
        }

        private class JoinReport : IEquatable<JoinReport>
        {
            public JoinReport(string completeName, UserReportDetailListItemViewModel oldReport, UserReportDetailListItemViewModel newReport, int rowNumber)
            {
                if (string.IsNullOrWhiteSpace(completeName))
                {
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(completeName));
                }

                this.CompleteName = completeName;
                this.OldReport = oldReport ?? throw new ArgumentNullException(nameof(oldReport));
                this.NewReport = newReport ?? throw new ArgumentNullException(nameof(newReport));
                this.RowNumber = rowNumber;
            }

            public string CompleteName { get; }
            public UserReportDetailListItemViewModel OldReport { get; }
            public UserReportDetailListItemViewModel NewReport { get; }
            public int RowNumber { get; }

            public bool Equals(JoinReport other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return this.CompleteName == other.CompleteName && Equals(this.OldReport, other.OldReport) && Equals(this.NewReport, other.NewReport) && this.RowNumber == other.RowNumber;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return this.Equals((JoinReport)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.CompleteName, this.OldReport, this.NewReport, this.RowNumber);
            }
        }

        private class JoinReportEqualityComparer : IEqualityComparer<JoinReport>
        {
            public bool Equals(JoinReport x, JoinReport y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return string.Equals(x.CompleteName, y.CompleteName, StringComparison.CurrentCultureIgnoreCase) && Equals(x.OldReport, y.OldReport) && Equals(x.NewReport, y.NewReport) && x.RowNumber == y.RowNumber;
            }

            public int GetHashCode(JoinReport obj)
            {
                return HashCode.Combine(obj.CompleteName, obj.OldReport, obj.NewReport, obj.RowNumber);
            }
        }
    }
}