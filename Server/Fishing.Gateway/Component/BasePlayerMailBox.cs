﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using GF.Unity.Common;
using Ps;

public class BasePlayerMailBox<TDef> : Component<TDef> where TDef : DefPlayerMailBox, new()
{
    //-------------------------------------------------------------------------
    BaseApp<DefApp> CoApp { get; set; }

    //-------------------------------------------------------------------------
    public override void init()
    {
        defNodeRpcMethod<PlayerMailBoxRequest>(
            (ushort)MethodType.c2sPlayerMailBoxRequest, c2sPlayerMailBoxRequest);

        CoApp = (BaseApp<DefApp>)Entity.getCacheData("CoApp");
    }

    //-------------------------------------------------------------------------
    public override void release()
    {
    }

    //-------------------------------------------------------------------------
    public override void update(float elapsed_tm)
    {
    }

    //-------------------------------------------------------------------------
    public override void handleEvent(object sender, EntityEvent e)
    {
    }

    //-------------------------------------------------------------------------
    async void c2sPlayerMailBoxRequest(PlayerMailBoxRequest mailbox_request)
    {
        IRpcSession s = EntityMgr.LastRpcSession;
        ClientInfo client_info = CoApp.getClientInfo(s);
        if (client_info == null) return;

        var task = await Task.Factory.StartNew<Task<MethodData>>(async () =>
        {
            MethodData method_data = new MethodData();
            method_data.method_id = MethodType.c2sPlayerMailBoxRequest;
            method_data.param1 = EbTool.protobufSerialize<PlayerMailBoxRequest>(mailbox_request);

            MethodData r = null;
            try
            {
                var grain_playerproxy = GrainClient.GrainFactory.GetGrain<ICellPlayer>(new Guid(client_info.et_player_guid));
                r = await grain_playerproxy.c2sRequest(method_data);
            }
            catch (Exception ex)
            {
                EbLog.Error(ex.ToString());
            }

            return r;
        });

        if (task.Status == TaskStatus.Faulted || task.Result == null)
        {
            if (task.Exception != null)
            {
                EbLog.Error(task.Exception.ToString());
            }

            return;
        }

        MethodData result = task.Result;
        if (result.method_id == MethodType.None)
        {
            return;
        }

        lock (CoApp.RpcLock)
        {
            var playersecretary_response = EbTool.protobufDeserialize<PlayerMailBoxResponse>(result.param1);
            CoApp.rpcBySession(s, (ushort)MethodType.s2cPlayerMailBoxResponse, playersecretary_response);
        }
    }
}
