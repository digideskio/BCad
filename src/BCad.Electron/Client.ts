// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc';
import { remote } from 'electron';

export default class Client {
    private _connection: rpc.MessageConnection;
    private _zoomInButton: HTMLButtonElement;
    private _zoomOutButton: HTMLButtonElement;
    private _openButton: HTMLButtonElement;
    private _outputPane: HTMLDivElement;

    // client requests
    private ZoomInRequest: rpc.RequestType0<void, void, void>;
    private ZoomOutRequest: rpc.RequestType0<void, void, void>;
    private ExecuteCommandRequest: rpc.RequestType1<string, void, void, void>;
    private GetDrawingRequest: rpc.RequestType2<number, number, string, void, void>;

    constructor() {
        this._zoomInButton = <HTMLButtonElement> document.getElementById('zoom-in-button');
        this._zoomOutButton = <HTMLButtonElement> document.getElementById('zoom-out-button');
        this._openButton = <HTMLButtonElement> document.getElementById('open-button');
        this._outputPane = <HTMLDivElement> document.getElementById('output-pane');
        let childProcess = cp.spawn('dotnet', ['node_modules/BCad.Server/BCad.Server.dll']);
        let logger: rpc.Logger = {
            error: console.log,
            warn: console.log,
            info: console.log,
            log: console.log,
        };
        this._connection = rpc.createMessageConnection(
            new rpc.StreamMessageReader(childProcess.stdout),
            new rpc.StreamMessageWriter(childProcess.stdin),
            logger);

        this.ZoomInRequest = new rpc.RequestType0<void, void, void>('ZoomIn');
        this.ZoomOutRequest = new rpc.RequestType0<void, void, void>('ZoomOut');
        this.ExecuteCommandRequest = new rpc.RequestType1<string, void, void, void>('ExecuteCommand');
        this.GetDrawingRequest = new rpc.RequestType2<number, number, string, void, void>('GetDrawing');

        this.prepareEvents();
        this._connection.listen();
    }
    private prepareEvents() {
        this._zoomInButton.addEventListener('click', () => {
            alert('zoom in');
            this._connection.sendRequest(this.ZoomInRequest).then(
                () => { alert('success on zoom in'); },
                (error) => { alert('error with then'); }
            );
            //this.updateDrawing();
        });
        this._zoomOutButton.addEventListener('click', () => {
            this._connection.sendRequest(this.ZoomOutRequest);
            this.updateDrawing();
        });
        this._openButton.addEventListener('click', () => {
            this._connection.sendRequest(this.ExecuteCommandRequest, 'File.Open');
            alert('just opened the file');
            this.updateDrawing();
        });

        let type = new rpc.RequestType<void, string, void, void>('GetFileNameFromUserForOpen');
        // file system service
        this._connection.onRequest(type, () => {
            let fileNames = remote.dialog.showOpenDialog({
                filters: [
                    { name: 'DXF Files', extensions: ['dxf', 'dxb'] },
                    { name: 'IGES Files', extensions: ['igs', 'iges'] }
                ]
            });

            return fileNames[0];
        });
    }
    public updateDrawing() {
        alert('about to update the drawing');
        this._connection.sendRequest(this.GetDrawingRequest, this._outputPane.clientWidth, this._outputPane.clientHeight).then((value) => {
            alert('just updated the drawing, setting UI');
            this._outputPane.innerHTML = value;
            alert(value);
        });
    }
}
