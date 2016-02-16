using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MinecraftManager.Extensions
{
    public static class ReactiveExtensions
    {

        /// <remarks>http://stackoverflow.com/a/731636/57883</remarks>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable) {
  var col = new ObservableCollection<T>();
  foreach ( var cur in enumerable ) {
    col.Add(cur);
  }
  return col;
}

    }
}
