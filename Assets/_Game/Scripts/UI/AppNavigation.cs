using System;
using System.Collections.Generic;

namespace GenericGachaRPG
{
    public enum AppRoute
    {
        Home,
        World,
        Gacha,
        Collection,
        Formation,
        Inventory,
        Missions,
        Settings,
        LockedFeature,
        Battle
    }

    public sealed class DemoUiRouter
    {
        private readonly Dictionary<AppRoute, DemoScreenView> views =
            new Dictionary<AppRoute, DemoScreenView>();
        private readonly Stack<AppRoute> history = new Stack<AppRoute>();
        private bool initialized;

        public AppRoute CurrentRoute { get; private set; } = AppRoute.Home;
        public string Context { get; private set; } = string.Empty;
        public bool CanGoBack => history.Count > 0;

        public event Action<AppRoute, string> RouteChanged;

        public void Register(AppRoute route, DemoScreenView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (views.ContainsKey(route))
            {
                throw new InvalidOperationException($"Route '{route}' is already registered.");
            }

            views.Add(route, view);
            view.SetVisible(false);
        }

        public bool IsRegistered(AppRoute route)
        {
            return views.ContainsKey(route);
        }

        public void Navigate(AppRoute route, string context = "", bool rememberCurrent = true)
        {
            if (!views.ContainsKey(route))
            {
                throw new InvalidOperationException($"Route '{route}' has no registered view.");
            }

            if (initialized && route == CurrentRoute &&
                string.Equals(Context, context ?? string.Empty, StringComparison.Ordinal))
            {
                RouteChanged?.Invoke(CurrentRoute, Context);
                return;
            }

            if (initialized && rememberCurrent && CurrentRoute != AppRoute.Battle)
            {
                history.Push(CurrentRoute);
            }

            ShowOnly(route);
            initialized = true;
            CurrentRoute = route;
            Context = context ?? string.Empty;
            RouteChanged?.Invoke(CurrentRoute, Context);
        }

        public void Replace(AppRoute route, string context = "")
        {
            Navigate(route, context, false);
        }

        public void ResetTo(AppRoute route, string context = "")
        {
            history.Clear();
            Navigate(route, context, false);
        }

        public void Back(AppRoute fallback = AppRoute.Home)
        {
            while (history.Count > 0)
            {
                AppRoute route = history.Pop();
                if (views.ContainsKey(route) && route != CurrentRoute)
                {
                    Navigate(route, string.Empty, false);
                    return;
                }
            }

            ResetTo(fallback);
        }

        private void ShowOnly(AppRoute route)
        {
            foreach (KeyValuePair<AppRoute, DemoScreenView> pair in views)
            {
                pair.Value.SetVisible(pair.Key == route);
            }
        }
    }
}
