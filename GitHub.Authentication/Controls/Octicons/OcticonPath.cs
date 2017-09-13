/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) GitHub Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GitHub.Authentication.Controls.Octicons;

namespace GitHub.UI
{
    /// <summary>
    /// Represent a raw path with no transformation. Uses the coordinate system from the octicon/svg
    /// files which are all drawn on a 1024px high canvas with variable width. If you're just after
    /// the shape this control can be used with Stretch=Uniform. If you're looking for an accurately
    /// scaled octicon correctly position you'll have to explicitly set the height of the path to
    /// 1024 and wrap it in a viewbox to scale it down to the size you want.
    /// </summary>
    public class OcticonPath : Shape
    {
        private static readonly Lazy<Dictionary<Octicon, Lazy<Geometry>>> cache =
            new Lazy<Dictionary<Octicon, Lazy<Geometry>>>(PrepareCache);

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(Octicon), typeof(OcticonPath),
            new FrameworkPropertyMetadata(defaultValue: Octicon.mark_github, flags:
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public Octicon Icon
        {
            get { return (Octicon)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        protected override Geometry DefiningGeometry
        {
            get { return GetGeometryForIcon(Icon); }
        }

        public static Geometry GetGeometryForIcon(Octicon icon)
        {
            var c = cache.Value;
            Lazy<Geometry> g;

            if (c.TryGetValue(icon, out g))
                return g.Value;

            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "Unknown Octicon: {0}", icon), nameof(icon));
        }

        // Initializes the cache dictionary with lazy entries for all available octicons
        private static Dictionary<Octicon, Lazy<Geometry>> PrepareCache()
        {
            return Enum.GetValues(typeof(Octicon))
                .Cast<Octicon>()
                .ToDictionary(icon => icon, icon => new Lazy<Geometry>(() => LoadGeometry(icon), LazyThreadSafetyMode.None));
        }

        private static Geometry LoadGeometry(Octicon icon)
        {
            var name = Enum.GetName(typeof(Octicon), icon);

            if (name == "lock")
                name = "_lock";

            var pathData = OcticonPaths.ResourceManager.GetString(name);

            if (pathData == null)
            {
                throw new ArgumentException("Could not find octicon geometry for '" + name + "'");
            }

            var path = PathGeometry.CreateFromGeometry(Geometry.Parse(pathData));

            if (path.CanFreeze)
                path.Freeze();

            return path;
        }
    }
}
