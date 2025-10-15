using System;
using System.Collections.Generic;
using ConsoleFramework.Controls;

namespace ConsoleFramework.Core;

public class VisualTreeHelper
{
    public static List<Control> FindAllChilds(Control control, Func<Control, bool> predicate)
    {
        if (null == control)
            throw new ArgumentNullException(nameof(control));
        if (null == predicate)
            throw new ArgumentNullException(nameof(predicate));

        List<Control> queue = new List<Control>();
        FindAllChildsRecoursively(queue, control, predicate);

        return queue;
    }

    private static void FindAllChildsRecoursively(List<Control> queue,
        Control control, Func<Control, bool> predicate)
    {
        foreach (Control child in control.Children)
        {
            if (predicate(child))
            {
                queue.Add(child);
            }

            FindAllChildsRecoursively(queue, child, predicate);
        }
    }

    /// <summary>
    /// Рекурсивно ищёт дочерний элемент по указанному Name.
    /// Если в результате поиска подходящий элемент не был найден, возвращается null.
    /// </summary>
    public static Control? FindChildByName(Control control, string childName)
    {
        if (null == control)
            throw new ArgumentNullException(nameof(control));
        if (string.IsNullOrEmpty(childName))
            throw new ArgumentException("String is null or empty", nameof(childName));
        //
        return FindChildByNameRecoursively(control, childName);
    }

    private static Control? FindChildByNameRecoursively(Control control, string childName)
    {
        IList<Control> children = control.Children;
        foreach (Control child in children)
        {
            if (child.Name == childName)
            {
                return child;
            }
            else
            {
                Control? result = FindChildByNameRecoursively(child, childName);
                if (null != result)
                    return result;
            }
        }

        return null;
    }

    public static bool IsConnectedToRoot(Control control)
    {
        if (null == control)
        {
            throw new ArgumentNullException(nameof(control));
        }

        Control root = ConsoleApplication.Instance.RootControl;
        Control? current = control;
        while (current != null)
        {
            if (current == root)
                return true;
            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Ищет ближайший родительский элемент контрола типа T.
    /// Возвращает его либо null, если такой не найден.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="control"></param>
    /// <returns></returns>
    public static T? FindClosestParent<T>(Control? control) where T : Control
    {
        Control? tmp = control;
        while (tmp != null && !(tmp is T))
            tmp = tmp.Parent;
        if (tmp is T) return (T)tmp;
        return null;
    }

    /// <summary>
    /// Находит самый верхний элемент под указателем мыши с координатами rawPoint.
    /// Учитывается прозрачность элементов - если пиксель, куда указывает мышь, отмечен как
    /// прозрачный для событий мыши (opacity от 4 до 7), то они будут проходить насквозь,
    /// к следующему контролу. Также учитывается видимость элементов - Hidden и Collapsed элементы
    /// будут проигнорированы.
    /// Так обрабатываются, например, тени окошек и прозрачные места контролов (первый столбец Combobox).
    /// </summary>
    /// <param name="localPoint">Координаты относительно control</param>
    /// <param name="control">RootElement для проверки всего визуального дерева.</param>
    /// <returns>Элемент управления или null, если событие мыши было за границами всех контролов, или
    /// если все контролы были прозрачны для событий мыши</returns>
    public static Control? FindTopControlUnderMouse(Control control, Point localPoint)
    {
        if (null == control) throw new ArgumentNullException(nameof(control));

        Point rawPoint = Control.TranslatePoint(control, localPoint, null);

        if (control.Children.Count != 0)
        {
            IList<Control> childrenOrderedByZIndex = control.GetChildrenOrderedByZIndex();
            for (int i = childrenOrderedByZIndex.Count - 1; i >= 0; i--)
            {
                Control child = childrenOrderedByZIndex[i];
                if (Control.HitTest(rawPoint, control, child))
                {
                    Control? foundSource = FindTopControlUnderMouse(child,
                        Control.TranslatePoint(control, localPoint, child));
                    if (null != foundSource) return foundSource;
                }
            }
        }

        Rect controlRect = new Rect(new Point(0, 0), control.RenderSize);
        if (!controlRect.Contains(localPoint))
        {
            return null;
        }

        if (control.Visibility != Visibility.Visible)
            return null;
        int opacity = ConsoleApplication.Instance.Renderer
            .GetControlOpacityAt(control, localPoint.X, localPoint.Y);
        if (opacity >= 4 && opacity <= 7)
        {
            return null;
        }

        return control;
    }
}