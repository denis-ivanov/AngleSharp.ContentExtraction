using System;
using System.Xml;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Text;

namespace AngleSharp.ContentExtraction
{
    public class ContentExtractor
    {
        private const string CharNumber = "char-number";
        private const string TagNumber = "tag-number";
        private const string LinkCharNumber = "linkchar-number";
        private const string LinkTagNumber = "linktag-number";
        private const string TextDensity = "text-density";
        private const string DensitySum = "density-sum";
        private const string MaxDensitySum = "max-density-sum";
        private const string Mark = "mark";

        protected virtual bool IgnoreElement(IElement element)
        {
            return element.TagName.Is(TagNames.NoScript) ||
                   element.TagName.Is(TagNames.Figcaption) ||
                   element.TagName.Is(TagNames.Figure) ||
                   element.TagName.Is(TagNames.Aside) ||
                   element.TagName.Is(TagNames.Footer) ||
                   element.TagName.Is(TagNames.Footer) ||
                   element.TagName.Is(TagNames.Header) ||
                   element.TagName.Is(TagNames.Svg) ||
                   element.GetStyle()?.GetDisplay() == "none";
        }

        protected virtual bool IgnoreNode(INode node)
        {
            return
                node is IHtmlBreakRowElement ||
                node is IHtmlHeadElement ||
                node is IHtmlHrElement ||
                node is IHtmlLinkElement ||
                node is IHtmlMetaElement ||
                node is IHtmlScriptElement ||
                node is IHtmlStyleElement ||
                node is IHtmlInlineFrameElement ||
                node is IHtmlFormElement ||
                node.NodeType == NodeType.Comment ||
                (node is IElement e && IgnoreElement(e));
        }

        protected virtual void ProcessDom(INode element)
        {
            var child = element.FirstChild;
            
            for (;child != null;)
            {
                if (IgnoreNode(child))
                {
                    var removeElement = child;
                    child = child.NextSibling;
                    removeElement.RemoveFromParent();
                    continue;
                }

                child = child.NextSibling;
            }

            for (child = element.FirstChild; child != null; child = child.NextSibling)
            {
                ProcessDom(child);
            }
        }

        protected virtual void RemoveAttribute(IElement element)
        {
            element.RemoveAttribute(CharNumber);
            element.RemoveAttribute(TagNumber);
            element.RemoveAttribute(LinkCharNumber);
            element.RemoveAttribute(LinkTagNumber);
            element.RemoveAttribute(TextDensity);
            element.RemoveAttribute(DensitySum);
            element.RemoveAttribute(MaxDensitySum);
            element.RemoveAttribute(Mark);

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                RemoveAttribute(child);
            }
        }

        protected virtual void CleanTreeByMark(IElement element)
        {
            var mark = XmlConvert.ToInt32(element.GetAttribute(Mark));

            if(0 == mark)
            {
                element.RemoveFromParent();
            }
            else if (1 == mark)
            {
                return;
            }
            else
            {
                for(var child = element.FirstElementChild; child != null;)
                {
                    var removeElement = child;
                    child = child.NextElementSibling;
                    CleanTreeByMark(removeElement);
                }
            }
        }

        protected virtual void CountChar(IElement element)
        {
            long charNum = element.TextContent.Length;
            var l2s_char_num = XmlConvert.ToString(charNum);
            element.SetAttribute(CharNumber, l2s_char_num);

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                CountChar(child);
            }
        }

        protected virtual void CountTag(IElement element)
        {
            long tag_num = 0;
            string l2s_tag_num;

            if(element.FirstElementChild == null)
            {
                l2s_tag_num = XmlConvert.ToString(0);
                element.SetAttribute(TagNumber, l2s_tag_num);
            }
            else
            {
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    CountTag(child);
                }
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    tag_num += XmlConvert.ToInt64(child.GetAttribute(TagNumber)) + 1;
                }

                l2s_tag_num = XmlConvert.ToString(tag_num);
                element.SetAttribute(TagNumber, l2s_tag_num);
            }
        }

        protected virtual void UpdateLinkChar(IElement element)
        {
            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                child.SetAttribute(LinkCharNumber, child.GetAttribute(CharNumber));
                UpdateLinkChar(child);
            }
        }

        protected virtual void CountLinkChar(IElement element)
        {
            long linkchar_num = 0;
            var tag_name = element.TagName;

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                CountLinkChar(child);
            }

            //deal with hyperlink and sth like that
            if(tag_name == TagNames.A || tag_name == TagNames.Button || tag_name == TagNames.Select)
            {
                linkchar_num = XmlConvert.ToInt64(element.GetAttribute(CharNumber));
                UpdateLinkChar(element);
            }
            else
            {
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    linkchar_num += XmlConvert.ToInt64(child.GetAttribute(LinkCharNumber));
                }
            }

            var l2s_linkchar_num = XmlConvert.ToString(linkchar_num);
            element.SetAttribute(LinkCharNumber, l2s_linkchar_num);
        }

        protected virtual void CountLinkTag(IElement element)
        {
            long linktag_num = 0;
            string l2s_linktag_num;
            var tag_name = element.TagName;

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                CountLinkTag(child);
            }

            //deal with hyperlink and sth like that
            if(tag_name == TagNames.A || tag_name == TagNames.Button || tag_name == TagNames.Select)
            {
                linktag_num = XmlConvert.ToInt64(element.GetAttribute(TagNumber));
                UpdateLinkChar(element);
            }
            else
            {
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    linktag_num += XmlConvert.ToInt64(child.GetAttribute(LinkTagNumber));
                    tag_name = child.TagName;

                    //if a tag is <a> or sth plays similar role in web pages, then anchor number add 1
                    if(tag_name == TagNames.A || tag_name == TagNames.Button || tag_name == TagNames.Select)
                    {
                        linktag_num++;
                    }
                    else
                    {
                        var child_linktag_num = XmlConvert.ToInt64(child.GetAttribute(LinkTagNumber));
                        var child_tag_num = XmlConvert.ToInt64(child.GetAttribute(TagNumber));
                        var child_char_num = XmlConvert.ToInt64(child.GetAttribute(CharNumber));
                        var child_linkchar_num = XmlConvert.ToInt64(child.GetAttribute(LinkCharNumber));

                        //child_linktag_num != 0: there are some anchor under this child
                        if(child_linktag_num == child_tag_num && child_char_num == child_linkchar_num && 0 != child_linktag_num)
                        {
                            linktag_num++;
                        }
                    }
                }
            }

            l2s_linktag_num = XmlConvert.ToString(linktag_num);
            element.SetAttribute(LinkTagNumber, l2s_linktag_num);
        }

        protected virtual void ComputeTextDensity(IElement element, double ratio)
        {
            var char_num = XmlConvert.ToInt64(element.GetAttribute(CharNumber));
            var tag_num = XmlConvert.ToInt64(element.GetAttribute(TagNumber));
            var linkchar_num = XmlConvert.ToInt64(element.GetAttribute(LinkCharNumber));
            var linktag_num = XmlConvert.ToInt64(element.GetAttribute(LinkTagNumber));

            var text_density = 0.0;
            string d2s_text_density;

            if(0 == char_num)
            {
                text_density = 0;
            }
            else
            {
                var un_linkchar_num = char_num - linkchar_num;

                if(0 == tag_num)
                {
                    tag_num = 1;
                }
                if(0 == linkchar_num)
                {
                    linkchar_num = 1;
                }
                if(0 == linktag_num)
                {
                    linktag_num = 1;
                }
                if(0 == un_linkchar_num)
                {
                    un_linkchar_num = 1;
                }
                
                text_density = (1.0 * char_num / tag_num) * Math.Log((1.0 * char_num * tag_num) / (1.0 * linkchar_num * linktag_num))
                               / Math.Log(Math.Log(1.0 * char_num * linkchar_num / un_linkchar_num + ratio * char_num + Math.Exp(1.0)));

//        text_density = 1.0 * char_num / tag_num;
            }

            //convert double to QString
            d2s_text_density = XmlConvert.ToString(text_density);
            element.SetAttribute(TextDensity, d2s_text_density);

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                ComputeTextDensity(child, ratio);
            }
        }

        protected virtual void ComputeDensitySum(IElement element, double ratio)
        {
            var densitySum = 0.0;
            //long char_num = 0;

            var content = element.TextContent;
            string child_content;
            var from = 0;
            var index = 0;
            var length = 0;

            if(element.FirstElementChild == null)
            {
                densitySum = XmlConvert.ToDouble(element.GetAttribute(TextDensity));
            }
            else
            {
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    ComputeDensitySum(child, ratio);
                }
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    densitySum += XmlConvert.ToDouble(child.GetAttribute(TextDensity));
                    XmlConvert.ToInt64(child.GetAttribute(CharNumber));

                    //text before tag
                    child_content = child.TextContent;
                    index = content.IndexOf(child_content, from, StringComparison.Ordinal);
                    if(index > -1)
                    {
                        length = index - from;
                        if(length > 0)
                        {
                            densitySum += length * Math.Log(1.0 * length) / Math.Log(Math.Log(ratio * length + Math.Exp(1.0)));
                        }
                        from = index + child_content.Length;
                    }
                }

                //text after tag
                length = element.TextContent.Length - from;
                if(length > 0)
                {
                    densitySum += length * Math.Log(1.0 * length) / Math.Log(Math.Log(ratio * length + Math.Exp(1.0)));
                }
            }

            var d2SDensitySum = XmlConvert.ToString(densitySum);
            element.SetAttribute(DensitySum, d2SDensitySum);
        }
        
        protected virtual double FindMaxDensitySum(IElement element)
        {
            var maxDensitySum = XmlConvert.ToDouble(element.GetAttribute(DensitySum));

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                var tempMaxDensitySum = FindMaxDensitySum(child);
                if(tempMaxDensitySum - maxDensitySum > double.Epsilon)
                {
                    maxDensitySum = tempMaxDensitySum;
                }
            }

            //record the max_density_sum under the element
            var d2SMaxDensitySum = XmlConvert.ToString(maxDensitySum);
            element.SetAttribute(MaxDensitySum, d2SMaxDensitySum);
            return maxDensitySum;
        }

        protected virtual IElement SearchTag(IElement element, string attribute, double value)
        {
            var d2SValue = XmlConvert.ToString(value);
            var target = element;

            var attrValue = XmlConvert.ToDouble(element.GetAttribute(attribute));
            if((attrValue - value > -1 * double.Epsilon)
                && (attrValue - value < double.Epsilon))
            {
                return target;
            }

            //search the max densitysum element using css selector
            var cssSelector = "[" + attribute + "=\"" + d2SValue + "\"]";
            target = element.QuerySelector(cssSelector);
            return target;
        }
        
        protected virtual double GetThreshold(IElement element, double maxDensitySum)
        {
            var threshold = -1.0;
        
            //search the max densitysum element
            var target = SearchTag(element, DensitySum, maxDensitySum);
            threshold = XmlConvert.ToDouble(target.GetAttribute(TextDensity));
            SetMark(target, 1);
        
            var parent = target.ParentElement;
            while(true)
            {
                if(parent.TagName != "HTML")
                {
                    var textDensity = XmlConvert.ToDouble(parent.GetAttribute(TextDensity));
                    if((threshold - textDensity) > -1 * double.Epsilon)
                    {
                        threshold = textDensity;
                    }
        
                    parent.SetAttribute(Mark, "2");
                    parent = parent.ParentElement;
                }
                else
                {
                    break;
                }
            }
        
            return threshold;
        }

        protected virtual void SetMark(IElement element, int mark)
        {
            var i2SMark = XmlConvert.ToString(mark);

            element.SetAttribute(Mark, i2SMark);

            for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
            {
                SetMark(child, mark);
            }
        }

        protected virtual void FindMaxDensitySumTag(IElement element, double maxDensitySum)
        {
            var target = SearchTag(element, DensitySum, maxDensitySum);

            var mark = XmlConvert.ToInt32(target.GetAttribute(Mark));
            if(1 == mark)
            {
                return;
            }

            SetMark(target, 1);

            var parent = target.ParentElement;
            while(true)
            {
                if(parent.TagName != "HTML")
                {
                    parent.SetAttribute(Mark, "2");
                    parent = parent.ParentElement;
                }
                else
                {
                    break;
                }
            }
        }

        protected virtual void MarkContent(IElement element, double threshold)
        {
            var textDensity = XmlConvert.ToDouble(element.GetAttribute(TextDensity));
            var maxDensitySum = XmlConvert.ToDouble(element.GetAttribute(MaxDensitySum));
            var mark = XmlConvert.ToInt32(element.GetAttribute(Mark));

            if(mark != 1 && (textDensity - threshold > -1 * double.Epsilon))
            {
                FindMaxDensitySumTag(element, maxDensitySum);
                for(var child = element.FirstElementChild; child != null; child = child.NextElementSibling)
                {
                    MarkContent(child, threshold);
                }
            }
        }

        public void Extract(IHtmlDocument document)
        {
            var body = document.Body;
            ProcessDom(body);

            CountChar(body);
            CountTag(body);
            CountLinkChar(body);
            CountLinkTag(body);

            var charNum = XmlConvert.ToDouble(body.GetAttribute(CharNumber));
            var linkCharNum = XmlConvert.ToDouble(body.GetAttribute(LinkCharNumber));

            if (linkCharNum < double.Epsilon)
            {
                linkCharNum = 1;
            }

            var ratio = linkCharNum / charNum;

            ComputeTextDensity(body, ratio);
            ComputeDensitySum(body, ratio);
            var maxDensitySum = FindMaxDensitySum(body);
            SetMark(body, 0);
            var threshold = GetThreshold(body, maxDensitySum);
            MarkContent(body, threshold);
            CleanTreeByMark(body);
            RemoveAttribute(body);
        }
    }
}
