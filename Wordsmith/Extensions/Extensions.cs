using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordsmith.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Move an item inside of a List of T.
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="list">The extended list</param>
        /// <param name="obj">The object to move</param>
        /// <param name="index">The index to move to. Offset will be calculated already. If out of range, obj will be moved to first or last slot.</param>
        public static void Move<T>(this List<T> list, T obj, int index)
        {
            // Get the current index of the object.
            int idx = list.IndexOf(obj);

            // If the current index is lower than the index we're moving to we actually
            // lower the new index by 1 because the indices will shift once we remove
            // the item from it's current place in the list.
            if (idx < index)
                --index;

            // If the index where we are moving the object to is the same as where it
            // is already located then simply return.
            else if (idx == index)
                return;

            // Remove the object
            list.Remove(obj);

            // As a failsafe, if the index is passed the end of the list, move the
            // object to the end of the list.
            if (index >= list.Count)
                list.Add(obj);

            // As a second failsafe, if the index is below zero, move the object to
            // the front of the list.
            else if (index < 0)
                list.Insert(0, obj);

            else
                // Insert it at the new location.
                list.Insert(index, obj);
        }


        /// <summary>
        /// Capitalizes the first letter in a string.
        /// </summary>
        /// <param name="s">The string to capitalize the first letter of.</param>
        /// <returns></returns>
        public static string CaplitalizeFirst(this string s)
        {
            if (s.Length == 1)
                return char.ToUpper(s[0]).ToString();
            else if (s.Length > 1)
                return char.ToUpper(s[0]).ToString() + s.Substring(1);
            return s;
        }

        public static string FixSpacing(this string s)
        {
            do
            {
                s = s.Replace("  ", " ");
            } while (s.Contains("  "));
            return s;
        }
    }
}
