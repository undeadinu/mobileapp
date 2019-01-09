using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.ViewHolders
{
    public class ReportsWorkspaceNameViewHolder : BaseRecyclerViewHolder<string>
    {
        private TextView workspaceName;

        public ReportsWorkspaceNameViewHolder(View itemView) : base(itemView)
        {
        }

        public ReportsWorkspaceNameViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            workspaceName = (TextView) ItemView;
        }

        protected override void UpdateView()
        {
            workspaceName.Text = Item;
        }
    }
}
