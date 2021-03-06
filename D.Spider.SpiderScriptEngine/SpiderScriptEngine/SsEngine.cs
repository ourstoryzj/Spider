﻿using System;
using System.Text.RegularExpressions;
using D.Spider.Core.Interface;
using Newtonsoft.Json.Linq;
using NSoup.Nodes;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace D.Spider.Core.SpiderScriptEngine
{
    /// <summary>
    /// ISpiderscriptEngine 实现
    /// </summary>
    public class SsEngine : ISpiderScriptEngine
    {
        /// <summary>
        /// 实现所有 KeywordHandler 的命名空间
        /// </summary>
        const string _handlerNamespce = "D.Spider.Core.SpiderScriptEngine.KeywordHandlers";

        ILogger _logger;

        Dictionary<SsKeywordTypes, ISsKeywordHandler> _keywordHandlers;

        public SsEngine(
            ILogger<SsEngine> logger
            )
        {
            _logger = logger;

            LoadAllKeywordHandlers();
        }

        #region ISpiderscriptEngine 接口实现
        public JToken Run(string html, string spiderscriptCode)
        {
            Document doc = NSoup.NSoupClient.Parse(html);

            _logger.LogDebug("SScode：" + spiderscriptCode);

            var context = AnalysisCodeString(spiderscriptCode);

            while (!context.CodeExecuteFinish)
            {
                var line = context.CodeLines[context.CurrDealLineIndex];

                _keywordHandlers[line.Type].Execute(context, line, doc, context.RootScope);
            }

            return context.ReturnObject.Data as JToken;
        }
        #endregion

        /// <summary>
        /// 通过反射加载所有的已经定义的 handler
        /// </summary>
        private void LoadAllKeywordHandlers()
        {
            _keywordHandlers = new Dictionary<SsKeywordTypes, ISsKeywordHandler>();

            var ass = Assembly.GetExecutingAssembly();

            var handlerTypes = (from t in ass.GetTypes()
                                where t.Namespace == _handlerNamespce && typeof(ISsKeywordHandler).IsAssignableFrom(t)
                                select t
                           ).ToList();

            foreach (var handlerType in handlerTypes)
            {
                var handler = ass.CreateInstance(handlerType.FullName) as ISsKeywordHandler;

                var t = handler.Type;

                if (_keywordHandlers.ContainsKey(t))
                {
                    throw new Exception("实现了重复的 ISsKeywordHandler " + t);
                }
                else
                {
                    _keywordHandlers.Add(t, handler);
                }
            }
        }

        /// <summary>
        /// 解析代码字符串
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private SsContext AnalysisCodeString(string code)
        {
            var lines = new SsCodeLines();

            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("SsEngine 没有设置页面解析 Code");
            }

            var slines = code
                .Replace("\r\n", "\n")
                .Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var handlers = _keywordHandlers.Values.OrderBy(h => h.Type);

            for (var i = 0; i < slines.Length; i++)
            {
                var sline = slines[i];

                var trimsline = sline.TrimStart();

                foreach (var handler in handlers)
                {
                    var line = handler.Analysis(trimsline);
                    if (line != null)
                    {
                        line.LineIndex = i;
                        line.SpaceCount = sline.Length - trimsline.Length;

                        lines.Add(line);
                        break;
                    }
                }
            }

            return new SsContext
            {
                CodeExecuteFinish = false,
                CodeLines = lines,
                CurrDealLineIndex = 0,
                ReturnObject = null,
                RootScope = new SsScope(),
                KeywordHandlers = _keywordHandlers
            };
        }

        #region 待删除

        /// <summary>
        /// 执行 Spiderscript 代码块
        /// </summary>
        /// <returns></returns>
        private SsVariable ExecuteCodeBlock(SsScope scope, Element ele, string[] lines)
        {
            for (var i = 0; i < lines.Length; i++)
            {

            }

            return null;
        }

        private void ExecuteCodeLine(SsScope scope, Element ele, string line)
        {
            if (Regex.IsMatch(line, "var"))
            {
                CreateVariable(scope, line);
            }
            else if (Regex.IsMatch(line, "foreach"))
            {
                ExecuteForeach(scope, ele, line);
            }
            else if (Regex.IsMatch(line, "[^']="))
            {

            }
            else if (Regex.IsMatch(line, "if"))
            {

            }
            else
            {
                _logger.LogWarning("不能处理代码行 " + line);
            }
        }

        private void ExecuteForeach(SsScope scope, Element ele, string line)
        {
            var w = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (w.Length != 2)
                throw new Exception(line + " foreach 格式错误");

            Regex reg = new Regex("'[^']'");
            var selectStr = reg.Match(w[1]).Groups[0].Value.Replace("'", "");

            foreach (var e in ele.Select(selectStr))
            {

            }
        }

        private void CreateVariable(SsScope scope, string line)
        {
            var w = line.Split(new char[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (w.Length != 3)
                throw new Exception(line + " 定义变量格式错误");

            var name = w[1];

            if (scope[name] != null)
                throw new Exception(name + " 重复定义");

            switch (w[2])
            {
                case "array":
                    scope[name] = new SsVariable()
                    {
                        Type = SsVariableTypes.SsArray,
                        Data = new JArray()
                    };
                    break;
                case "object":
                    scope[name] = new SsVariable()
                    {
                        Type = SsVariableTypes.SsObject,
                        Data = new JObject()
                    };
                    break;
            }
        }
        #endregion
    }
}
