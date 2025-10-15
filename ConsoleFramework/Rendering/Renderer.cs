using System;
using System.Collections.Generic;
using ConsoleFramework.Controls;
using ConsoleFramework.Core;

namespace ConsoleFramework.Rendering;

/// <summary>
/// Central point of the console framework layout system.
/// </summary>
public sealed class Renderer
{
    private Rect _rootElementRect;

    /// <summary>
    /// Прямоугольная область относительно экрана консоли, в которой будет размещён Root Element.
    /// </summary>
    public Rect RootElementRect
    {
        get => _rootElementRect;
        set
        {
            if (_rootElementRect != value)
            {
                _rootElementRect = value;
                if (null != RootElement)
                    AddControlToInvalidationQueue(RootElement);
            }
        }
    }

    private Control? _rootElement;

    public Control? RootElement
    {
        get => _rootElement;
        set
        {
            if (_rootElement != value)
            {
                if (_rootElement != null)
                    _rootElement.ControlUnsetAsRootElement();
                _rootElement = value;
                if (_rootElement != null)
                    _rootElement.ControlSetAsRootElement();
            }
        }
    }

    public PhysicalCanvas? Canvas { get; set; }

    // Buffers containing only control rendering representation itself
    private readonly Dictionary<Control, RenderingBuffer> _buffers = new Dictionary<Control, RenderingBuffer>();

    // Buffers containing full control render (with children render applied)
    private readonly Dictionary<Control, RenderingBuffer> _fullBuffers = new Dictionary<Control, RenderingBuffer>();

    // Queue of controls marked for layout invalidation
    private readonly List<Control> _invalidatedControls = new List<Control>();

    /// <summary>
    /// Контролы, в дочерних элементах которого были изменения в порядке Z-Order
    /// (только Z-Order, если были добавлены или удалены дочерние - то он автоматически
    /// будет invalidated, и в этот список добавлять уже не нужно).
    /// </summary>
    private readonly List<Control> _zOrderCheckControls = new List<Control>();

    public bool AnyControlInvalidated => _invalidatedControls.Count != 0;

    // список контролов, у которых обновилось содержимое full render buffer
    // актуален только при вызовах UpdateLayout, после вызова FinallyApplyChangesToCanvas
    // очищается
    private readonly List<Control> _renderingUpdatedControls = new List<Control>();

    private enum AffectType
    {
        LayoutInvalidated,
        LayoutRevalidated
    }

    private struct ControlAffectInfo
    {
        public readonly Control Control;
        public readonly AffectType AffectType;

        public ControlAffectInfo(Control control, AffectType affectType)
        {
            this.Control = control;
            this.AffectType = affectType;
        }
    }

    /// <summary>
    /// Сбрасывает все изменения, накопленные в течение предыдущих вызовов
    /// UpdateLayout, на экран.
    /// </summary>
    public void FinallyApplyChangesToCanvas(bool forceRepaintAll = false)
    {
        Rect affectedRect = Rect.Empty;

        // Propagate updated rendered buffers to parent elements and eventually to Canvas
        foreach (Control control in _renderingUpdatedControls)
        {
            Rect currentAffectedRect = ApplyChangesToCanvas(control, new Rect(new Point(0, 0), control.RenderSize));
            affectedRect.Union(currentAffectedRect);
        }

        if (forceRepaintAll)
        {
            affectedRect = new Rect(_rootElementRect.Size);
        }

        // Flush stored image (with this.RootElementRect offset)
        if (!affectedRect.IsEmpty)
        {
            // Affected rect relative to canvas
            Rect affectedRectAbsolute = new Rect(affectedRect.X + RootElementRect.X, affectedRect.Y + RootElementRect.Y,
                affectedRect.Width, affectedRect.Height);

            if (Canvas == null)
                throw new InvalidOperationException("Canvas is not set.");

            // Clip according to real canvas size
            affectedRectAbsolute.Intersect(new Rect(new Point(0, 0), Canvas.Size));

            Canvas.Flush(affectedRectAbsolute);
        }

        // If anything changed in layout - update displaying cursor state
        if (_renderingUpdatedControls.Count > 0)
        {
            ConsoleApplication.Instance.FocusManager.RefreshMouseCursor();
        }

        // Prepare for next layout pass
        _renderingUpdatedControls.Clear();
    }

    /// <summary>
    /// Пересчитывает лайаут для всех контролов, добавленных в очередь ревалидации.
    /// Определяет, какие контролы необходимо перерисовать, вызывает Render у них.
    /// Определяет, какие области экрана необходимо обновить и выполняет перерисовку
    /// экрана консоли.
    /// </summary>
    public void UpdateLayout()
    {
        List<ControlAffectInfo> affectedControls = new List<ControlAffectInfo>();

        // Invalidate layout and fill renderingUpdatedControls list
        InvalidateLayout(affectedControls);

        // Raise all invalidated and revalidated events of affected controls with subscribers
        foreach (ControlAffectInfo affectInfo in affectedControls)
        {
            if (affectInfo.AffectType == AffectType.LayoutInvalidated)
                affectInfo.Control.RaiseInvalidatedEvent();
            else if (affectInfo.AffectType == AffectType.LayoutRevalidated)
                affectInfo.Control.RaiseRevalidatedEvent();
        }

        // Перебираем zOrderCheckControls, для каждого контрола проверяя все его дочерние -
        // не изменился ли их overlappedRect ? Если да, и изменился так, что
        // бОльшая часть дочернего контрола стала видима - добавить этот контрол в список
        // renderingUpdatedControls. Их содержимое после этого в методе FinallyApplyChangesToCanvas
        // будет выведено на экран.
        foreach (Control zOrderCheckControl in _zOrderCheckControls)
        {
            RefreshChildrenLastOverlappedRects(zOrderCheckControl, true);
        }

        // Clear list to prepare for next layout pass
        _zOrderCheckControls.Clear();
    }

    /// <summary>
    /// Обновляет LastOverlappedRect у всех контролов, которые являются непосредственными
    /// детьми parent'a, в соответствии с их Z-Order. Если addToInvalidatedIfChanged = true,
    /// то те дочерние элементы, у которых OverlappedRect уменьшился по сравнению с предыдущим
    /// значением, будут добавлены в список renderingUpdatedControls.
    /// </summary>
    private void RefreshChildrenLastOverlappedRects(Control parent,
        bool addToInvalidatedIfChanged)
    {
        for (int i = 0; i < parent.Children.Count; i++)
        {
            Control control = parent.Children[i];
            // Относительно parent
            Rect controlRect = control.RenderSlotRect;
            // Относительно control
            Rect overlappedRect = Rect.Empty;

            // Проверяем только тех соседей, у которых Z-Order выше
            for (int j = i + 1; j < parent.Children.Count; j++)
            {
                Control sibling = parent.Children[j];
                if (sibling != control)
                {
                    if (controlRect.IntersectsWith(sibling.RenderSlotRect))
                    {
                        Rect controlRectCopy = controlRect;
                        controlRectCopy.Intersect(sibling.RenderSlotRect);
                        if (!controlRectCopy.IsEmpty)
                        {
                            controlRectCopy.Offset(-controlRect.X, -controlRect.Y);
                            overlappedRect.Union(controlRectCopy);
                        }
                    }
                }
            }

            if (addToInvalidatedIfChanged)
            {
                Rect lastOverlappedRectCopy = control.LastOverlappedRect;
                lastOverlappedRectCopy.Union(overlappedRect);

                // Only add to invalidated if new rect is not inside old
                if (lastOverlappedRectCopy != overlappedRect)
                {
                    _renderingUpdatedControls.Add(control);
                }
            }

            control.LastOverlappedRect = overlappedRect;
        }
    }

    /// <summary>
    /// Получает для указанного контрола full render buffer и применяет его последовательно
    /// ко всем родительским элементам управления, вплоть до изображения на экране.
    /// Возвращает прямоугольник, необходимый для ревалидации на экране (affected rect).
    /// Учитывает Z-Order контролов-соседей (если родительский контрол имеет несколько дочерних, они могут перекрывать
    /// друг друга).
    /// Первый вызов производится с affectedRect = control.RenderSize.
    /// </summary>
    /// <returns>Affected rectangle in canvas should be copyied to console screen.</returns>
    private Rect ApplyChangesToCanvas(Control control, Rect affectedRect)
    {
        // если системой лайаута были определены размеры дочернего контрола, превышающие размеры слота
        // (такое может произойти, если дочерний контрол игнорирует переданные аргументы в MeasureOverride
        // и ArrangeOverride), то в этом месте может прийти affectedRect, выходящий за рамки
        // текущего RenderSize контрола, и мы должны выполнить intersection для корректного наложения
        affectedRect.Intersect(new Rect(new Point(0, 0), control.RenderSize));
        RenderingBuffer fullBuffer = GetOrCreateFullBufferForControl(control);
        if (control.Parent != null)
        {
            RenderingBuffer fullParentBuffer = GetOrCreateFullBufferForControl(control.Parent);
            // если буфер контрола содержит opacity пиксели в affectedRect, то мы вынуждены переинициализировать
            // буфер парента целиком (не вызывая Render, конечно, но переналожением буферов дочерних элементов)
            if (fullBuffer.ContainsOpacity(affectedRect))
            {
                fullParentBuffer.Clear();
                fullParentBuffer.CopyFrom(GetOrCreateBufferForControl(control.Parent));
                foreach (Control child in control.Parent.Children)
                {
                    if (child.Visibility == Visibility.Visible)
                    {
                        RenderingBuffer childBuffer = GetOrCreateFullBufferForControl(child);
                        fullParentBuffer.ApplyChild(childBuffer, child.ActualOffset,
                            child.RenderSize, child.RenderSlotRect, child.LayoutClip);
                    }
                }
            }

            if (control.Visibility == Visibility.Visible)
            {
                if (affectedRect == new Rect(new Point(0, 0), control.RenderSize))
                {
                    fullParentBuffer.ApplyChild(fullBuffer, control.ActualOffset,
                        control.RenderSize, control.RenderSlotRect, control.LayoutClip);
                }
                else
                {
                    fullParentBuffer.ApplyChild(fullBuffer, control.ActualOffset,
                        control.RenderSize, control.RenderSlotRect, control.LayoutClip,
                        affectedRect);
                }
            }

            // определим соседей контрола, которые могут перекрывать его
            IList<Control> neighbors = control.Parent.GetChildrenOrderedByZIndex();

            // восстанавливаем изображение поверх обновленного контрола, если
            // имеются контролы, лежащие выше по z-order
            int controlIndex = neighbors.IndexOf(control);
            // начиная с controlIndex + 1 в списке лежат контролы с z-index больше чем z-index текущего контрола
            for (int i = controlIndex + 1; i < neighbors.Count; i++)
            {
                Control neighbor = neighbors[i];
                fullParentBuffer.ApplyChild(GetOrCreateFullBufferForControl(neighbor),
                    neighbor.ActualOffset, neighbor.RenderSize,
                    neighbor.RenderSlotRect, neighbor.LayoutClip);
            }

            Rect parentAffectedRect = control.RenderSlotRect;
            parentAffectedRect.Intersect(new Rect(affectedRect.X + control.ActualOffset.X,
                affectedRect.Y + control.ActualOffset.Y,
                affectedRect.Width,
                affectedRect.Height));
            // нет смысла продолжать подъем вверх по дереву, если контрола точно уже не видно
            if (parentAffectedRect.IsEmpty)
            {
                return Rect.Empty;
            }

            return ApplyChangesToCanvas(control.Parent, parentAffectedRect);
        }
        else
        {
            if (control != RootElement)
                throw new InvalidOperationException("Assertion failed.");

            if (Canvas == null)
                throw new InvalidOperationException("Canvas is not set.");
            // мы добрались до экрана консоли
            fullBuffer.CopyToPhysicalCanvas(Canvas, affectedRect, RootElementRect.TopLeft);
            return affectedRect;
        }
    }

    /// <summary>
    /// Пересчитывает лайаут для всех контролов, добавленных в очередь ревалидации.
    /// После того, как лайаут контрола рассчитан, выполняется рендеринг.
    /// Рендеринг производится только тогда, когда размеры контрола изменились или
    /// контрол явно помечен как изменивший свое изображение. В остальных случаях
    /// используются кешированные буферы, содержащие уже отрендеренные изображения.
    /// </summary>
    /// <param name="affectedControls"></param>
    private void InvalidateLayout(List<ControlAffectInfo> affectedControls)
    {
        List<Control> resetControls = new List<Control>();
        List<Control> revalidatedControls = new List<Control>();
        while (_invalidatedControls.Count != 0)
        {
            // Dequeue next control
            Control control = _invalidatedControls[_invalidatedControls.Count - 1];
            _invalidatedControls.RemoveAt(_invalidatedControls.Count - 1);

            // Set previous results of layout passes dirty
            control.ResetValidity(resetControls);
            if (resetControls.Count > 0)
            {
                foreach (Control resetControl in resetControls)
                {
                    affectedControls.Add(new ControlAffectInfo(resetControl, AffectType.LayoutInvalidated));
                }

                resetControls.Clear();
            }

            //
            UpdateLayout(control, revalidatedControls);
            if (revalidatedControls.Count > 0)
            {
                foreach (Control revalidatedControl in revalidatedControls)
                {
                    affectedControls.Add(new ControlAffectInfo(revalidatedControl, AffectType.LayoutRevalidated));
                }

                revalidatedControls.Clear();
            }
        }
    }

    private bool CheckDesiredSizeNotChangedRecursively(Control control)
    {
        if (control.LastLayoutInfo.UnclippedDesiredSize != control.LayoutInfo.UnclippedDesiredSize)
        {
            return false;
        }

        foreach (Control child in control.Children)
        {
            if (!CheckDesiredSizeNotChangedRecursively(child))
                return false;
        }

        return true;
    }

    private void UpdateLayout(Control control, List<Control> revalidatedControls)
    {
        LayoutInfo lastLayoutInfo = control.LastLayoutInfo;
        // работаем с родительским элементом управления
        if (control.Parent != null)
        {
            bool needUpdateParentLayout = true;
            // если размер текущего контрола не изменился, то состояние ревалидации не распространяется
            // вверх по дереву элементов, и мы переходим к работе с дочерними элементами
            // в противном случае мы добавляем родительский элемент в конец очереди ревалидации, и
            // возвращаем управление
            if (lastLayoutInfo.Validity != LayoutValidity.Nothing)
            {
                control.Measure(lastLayoutInfo.MeasureArgument);
//                    if (lastLayoutInfo.unclippedDesiredSize == control.layoutInfo.unclippedDesiredSize) {
                if (CheckDesiredSizeNotChangedRecursively(control))
                {
                    needUpdateParentLayout = false;
                }
            }

            if (needUpdateParentLayout)
            {
                // mark the parent control for invalidation too and enqueue them
                control.Parent.Invalidate();
                // мы можем закончить с этим элементом, поскольку мы уже добавили
                // в конец очереди его родителя, и мы все равно вернемся к нему в след. раз
                return;
            }
        }

        // работаем с дочерними элементами управления
        // вызываем для текущего контрола Measure&Arrange с последними значениями аргументов
        if (lastLayoutInfo.Validity == LayoutValidity.Nothing && control.Parent != null)
        {
            throw new InvalidOperationException("Assertion failed.");
        }

        // rootElement - особый случай
        if (control.Parent == null)
        {
            if (control != RootElement)
            {
                throw new InvalidOperationException("Control has no parent but is not known rootElement.");
            }

            control.Measure(RootElementRect.Size);
            control.Arrange(RootElementRect);
        }
        else
        {
            control.Measure(lastLayoutInfo.MeasureArgument);
            control.Arrange(lastLayoutInfo.RenderSlotRect);
        }

        // update render buffers of current control and its children
        RenderingBuffer buffer = GetOrCreateBufferForControl(control);
        RenderingBuffer fullBuffer = GetOrCreateFullBufferForControl(control);
        // replace buffers if control has grown
        LayoutInfo layoutInfo = control.LayoutInfo;
        if (layoutInfo.RenderSize.Width > buffer.Width || layoutInfo.RenderSize.Height > buffer.Height)
        {
            buffer = new RenderingBuffer(layoutInfo.RenderSize.Width, layoutInfo.RenderSize.Height);
            fullBuffer = new RenderingBuffer(layoutInfo.RenderSize.Width, layoutInfo.RenderSize.Height);
            _buffers[control] = buffer;
            _fullBuffers[control] = fullBuffer;
        }

        buffer.Clear();
        if (control.RenderSize.Width != 0 && control.RenderSize.Height != 0)
            control.Render(buffer);
        // проверяем дочерние контролы - если их layoutInfo не изменился по сравнению с последним,
        // то мы можем взять их последний renderBuffer без обновления и применить к текущему контролу
        fullBuffer.CopyFrom(buffer);
        IList<Control> children = control.Children;
        foreach (Control child in children)
        {
            if (child.Visibility == Visibility.Visible)
            {
                RenderingBuffer fullChildBuffer = ProcessControl(child, revalidatedControls);
                fullBuffer.ApplyChild(fullChildBuffer, child.ActualOffset,
                    child.RenderSize,
                    child.RenderSlotRect, child.LayoutClip);
            }
            else
            {
                // чтобы следующий Invalidate перезаписал lastLayoutInfo
                if (child.SetValidityToRender())
                {
                    revalidatedControls.Add(child);
                }
            }
        }

        // Save overlappingRect for each control child
        RefreshChildrenLastOverlappedRects(control, false);

        if (control.SetValidityToRender())
        {
            revalidatedControls.Add(control);
        }

        AddControlToRenderingUpdatedList(control);
    }

    /// <summary>
    /// Добавляет указанный контрол в список контролов, для которых обновлен full rendering buffer.
    /// </summary>
    private void AddControlToRenderingUpdatedList(Control control)
    {
        _renderingUpdatedControls.Add(control);
    }

    private bool CheckRenderingWasNotChangedRecursively(Control control)
    {
        if (!control.LastLayoutInfo.Equals(control.LayoutInfo)
            || control.LastLayoutInfo.Validity != LayoutValidity.Render) return false;
        foreach (Control child in control.Children)
        {
            if (!CheckRenderingWasNotChangedRecursively(child)) return false;
        }

        return true;
    }

    private RenderingBuffer ProcessControl(Control control, List<Control> revalidatedControls)
    {
        RenderingBuffer buffer = GetOrCreateBufferForControl(control);
        RenderingBuffer fullBuffer = GetOrCreateFullBufferForControl(control);
        //
        LayoutInfo lastLayoutInfo = control.LastLayoutInfo;
        LayoutInfo layoutInfo = control.LayoutInfo;
        //
        control.Measure(lastLayoutInfo.MeasureArgument);
        control.Arrange(lastLayoutInfo.RenderSlotRect);
        // if lastLayoutInfo eq layoutInfo we can use last rendered buffer
        if (CheckRenderingWasNotChangedRecursively(control))
        {
            if (control.SetValidityToRender())
            {
                revalidatedControls.Add(control);
            }

            return fullBuffer;
        }

        // replace buffers if control has grown
        if (layoutInfo.RenderSize.Width > buffer.Width || layoutInfo.RenderSize.Height > buffer.Height)
        {
            buffer = new RenderingBuffer(layoutInfo.RenderSize.Width, layoutInfo.RenderSize.Height);
            fullBuffer = new RenderingBuffer(layoutInfo.RenderSize.Width, layoutInfo.RenderSize.Height);
            _buffers[control] = buffer;
            _fullBuffers[control] = fullBuffer;
        }

        // otherwise we should assemble full rendered buffer using childs
        buffer.Clear();
        if (control.RenderSize.Width != 0 && control.RenderSize.Height != 0)
            control.Render(buffer);
        //
        fullBuffer.CopyFrom(buffer);
        foreach (Control child in control.Children)
        {
            if (child.Visibility == Visibility.Visible)
            {
                RenderingBuffer fullChildBuffer = ProcessControl(child, revalidatedControls);
                fullBuffer.ApplyChild(fullChildBuffer, child.ActualOffset,
                    child.RenderSize, child.RenderSlotRect, child.LayoutClip);
            }
            else
            {
                // чтобы следующий Invalidate для этого контрола
                // перезаписал lastLayoutInfo
                if (child.SetValidityToRender())
                {
                    revalidatedControls.Add(child);
                }
            }
        }

        // Save overlappingRect for each control child
        RefreshChildrenLastOverlappedRects(control, false);

        if (control.SetValidityToRender())
        {
            revalidatedControls.Add(control);
        }

        return fullBuffer;
    }

    internal void AddControlToInvalidationQueue(Control control)
    {
        if (null == control) throw new ArgumentNullException(nameof(control));
        if (!_invalidatedControls.Contains(control))
        {
            // Add to queue only if it has parent, or it is root element
            if (control.Parent != null || control == RootElement)
            {
                _invalidatedControls.Add(control);
            }
        }
    }

    private RenderingBuffer GetOrCreateBufferForControl(Control control)
    {
        if (_buffers.TryGetValue(control, out var value))
        {
            return value;
        }

        RenderingBuffer buffer = new RenderingBuffer(control.ActualWidth, control.ActualHeight);
        _buffers.Add(control, buffer);
        return buffer;
    }

    private RenderingBuffer GetOrCreateFullBufferForControl(Control control)
    {
        if (_fullBuffers.TryGetValue(control, out var value))
        {
            return value;
        }

        RenderingBuffer buffer = new RenderingBuffer(control.ActualWidth, control.ActualHeight);
        _fullBuffers.Add(control, buffer);
        return buffer;
    }

    /// <summary>
    /// Возващает код прозрачности контрола в указанной точке.
    /// Это необходимо для определения контрола, который станет источником события мыши.
    /// </summary>
    internal int GetControlOpacityAt(Control control, int x, int y)
    {
        // Если контрол, над которым водят мышью, имеет невидимых сыновей, которые ни разу
        // не отрисовывались, то в словаре буферов для таких сыновей ничего не окажется.
        // Возвращаем для таких детей 6 - как будто они полностью прозрачны
        if (!_buffers.ContainsKey(control))
        {
            return 6;
        }

        return _buffers[control].GetOpacityAt(x, y);
    }

    /// <summary>
    /// Called when control is removed from visual tree.
    /// It is necessary to remove it from invalidated queue if they are there.
    /// </summary>
    internal void ControlRemovedFromTree(Control child)
    {
        if (_invalidatedControls.Contains(child))
        {
            _invalidatedControls.Remove(child);
        }

        foreach (var nestedChild in child.Children)
        {
            ControlRemovedFromTree(nestedChild);
        }
    }

    /// <summary>
    /// Called when some of control's children changed their z-order.
    /// (Not when added or removed - just changed between them).
    /// This call allows layout system to detect when need to refresh
    /// display image if no controls invalidated but z-order changed.
    /// </summary>
    internal void AddControlToZOrderCheckList(Control control)
    {
        _zOrderCheckControls.Add(control);
    }
}