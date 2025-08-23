using UnityEngine.UIElements;

namespace UAI
{
    // Base class for tab pages
    public abstract class TabPageBase
    {
        protected ImageCreatorWindow m_Window;
        protected VisualElement m_Root;

        public TabPageBase(ImageCreatorWindow window)
        {
            m_Window = window;
        }

        public abstract VisualElement CreateUI();
        public abstract void SaveSettings();
        public abstract void LoadSettings();
    } 
}
