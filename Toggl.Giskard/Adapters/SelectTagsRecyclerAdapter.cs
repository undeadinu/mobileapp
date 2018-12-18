using System;
using System.Windows.Input;
using Android.Runtime;
using Toggl.Foundation;
using Toggl.Giskard.TemplateSelectors;

namespace Toggl.Giskard.Adapters
{
    public sealed class SelectTagsRecyclerAdapter : SelectCreatableEntityRecyclerAdapter
    {
        public SelectTagsRecyclerAdapter()
        {
        }

        public SelectTagsRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override string SuggestingItemText
            => $"{Resources.CreateTag} \"{Text.Trim()}\"";

        protected override ICommand GetClickCommand(int viewType) => 
            viewType == SelectTagsTemplateSelector.CreateEntity ? CreateCommand : ItemClick;
    }
}
