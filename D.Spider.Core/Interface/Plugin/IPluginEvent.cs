﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.Spider.Core.Interface
{
    /// <summary>
    /// 插件事件
    /// </summary>
    public interface IPluginEvent
    {
        /// <summary>
        /// 事件的唯一标识
        /// </summary>
        Guid Uid { get; }

        /// <summary>
        /// 来源（唯一），当来自爬虫自己时，这个值为 null
        /// </summary>
        IPluginSymbol FromPlugin { get; }

        /// <summary>
        /// 目标（不唯一，可以是某一个类型的插件）
        /// </summary>
        IEnumerable<IPluginSymbol> ToPluginSymbols { get; }

        /// <summary>
        /// 创建时间
        /// </summary>
        DateTime CreateTime { get; }

        /// <summary>
        /// 事件状态
        /// </summary>
        PluginEventState State { get; set; }

        /// <summary>
        /// 响应事件的插件个数
        /// </summary>
        DealPluginEventType DealType { get; }
    }
}
