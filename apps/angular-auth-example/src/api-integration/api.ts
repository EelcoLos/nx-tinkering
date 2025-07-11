//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v14.4.0.0 (NJsonSchema v11.3.2.0 (Newtonsoft.Json v13.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

/* tslint:disable */
/* eslint-disable */
// ReSharper disable InconsistentNaming

import { mergeMap as _observableMergeMap, catchError as _observableCatch } from 'rxjs/operators';
import { Observable, throwError as _observableThrow, of as _observableOf } from 'rxjs';
import { Injectable, InjectionToken, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse, HttpResponseBase } from '@angular/common/http';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

export interface IClient {
    /**
     * @return Success
     */
    login(loginRequest: LoginRequest): Observable<LoginResponse>;
    /**
     * @return Success
     */
    createuser(myRequest: MyRequest): Observable<MyResponse>;
    /**
     * @return Success
     */
    validatetoken(validateTokenRequest: ValidateTokenRequest): Observable<ValidateTokenResponse>;
}

@Injectable({
    providedIn: 'root'
})
export class Client implements IClient {
    private http: HttpClient;
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor() {
        const http = inject<HttpClient>(HttpClient);
        const baseUrl = inject(API_BASE_URL, { optional: true });

        this.http = http;
        this.baseUrl = baseUrl ?? "";
    }

    /**
     * @return Success
     */
    login(loginRequest: LoginRequest): Observable<LoginResponse> {
        let url_ = this.baseUrl + "/api/login";
        url_ = url_.replace(/[?&]$/, "");

        const content_ = JSON.stringify(loginRequest);

        let options_ : any = {
            body: content_,
            observe: "response",
            responseType: "blob",
            headers: new HttpHeaders({
                "Content-Type": "application/json",
                "Accept": "application/json"
            })
        };

        return this.http.request("post", url_, options_).pipe(_observableMergeMap((response_ : any) => {
            return this.processLogin(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processLogin(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<LoginResponse>;
                }
            } else
                return _observableThrow(response_) as any as Observable<LoginResponse>;
        }));
    }

    protected processLogin(response: HttpResponseBase): Observable<LoginResponse> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;

        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); }}
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            let result200: any = null;
            result200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver) as LoginResponse;
            return _observableOf(result200);
            }));
        } else if (status === 400) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            let result400: any = null;
            result400 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver) as ErrorResponse;
            return throwException("Bad Request", status, _responseText, _headers, result400);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    /**
     * @return Success
     */
    createuser(myRequest: MyRequest): Observable<MyResponse> {
        let url_ = this.baseUrl + "/api/user/create";
        url_ = url_.replace(/[?&]$/, "");

        const content_ = JSON.stringify(myRequest);

        let options_ : any = {
            body: content_,
            observe: "response",
            responseType: "blob",
            headers: new HttpHeaders({
                "Content-Type": "application/json",
                "Accept": "application/json"
            })
        };

        return this.http.request("post", url_, options_).pipe(_observableMergeMap((response_ : any) => {
            return this.processCreateuser(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processCreateuser(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<MyResponse>;
                }
            } else
                return _observableThrow(response_) as any as Observable<MyResponse>;
        }));
    }

    protected processCreateuser(response: HttpResponseBase): Observable<MyResponse> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;

        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); }}
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            let result200: any = null;
            result200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver) as MyResponse;
            return _observableOf(result200);
            }));
        } else if (status === 401) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("Unauthorized", status, _responseText, _headers);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    /**
     * @return Success
     */
    validatetoken(validateTokenRequest: ValidateTokenRequest): Observable<ValidateTokenResponse> {
        let url_ = this.baseUrl + "/api/validate-token";
        url_ = url_.replace(/[?&]$/, "");

        const content_ = JSON.stringify(validateTokenRequest);

        let options_ : any = {
            body: content_,
            observe: "response",
            responseType: "blob",
            headers: new HttpHeaders({
                "Content-Type": "application/json",
                "Accept": "application/json"
            })
        };

        return this.http.request("post", url_, options_).pipe(_observableMergeMap((response_ : any) => {
            return this.processValidatetoken(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processValidatetoken(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<ValidateTokenResponse>;
                }
            } else
                return _observableThrow(response_) as any as Observable<ValidateTokenResponse>;
        }));
    }

    protected processValidatetoken(response: HttpResponseBase): Observable<ValidateTokenResponse> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;

        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); }}
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            let result200: any = null;
            result200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver) as ValidateTokenResponse;
            return _observableOf(result200);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }
}

export interface LoginResponse {
    token?: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface ErrorResponse {
    statusCode?: number;
    message?: string;
    errors?: { [key: string]: string[]; };
}

export interface MyResponse {
    fullName?: string;
    isOver18?: boolean;
}

export interface MyRequest {
    firstName?: string;
    lastName?: string;
    age?: number;
}

export interface ValidateTokenResponse {
    isValid?: boolean;
}

export interface ValidateTokenRequest {
    token?: string;
}

export class SwaggerException extends Error {
    override message: string;
    status: number;
    response: string;
    headers: { [key: string]: any; };
    result: any;

    constructor(message: string, status: number, response: string, headers: { [key: string]: any; }, result: any) {
        super();

        this.message = message;
        this.status = status;
        this.response = response;
        this.headers = headers;
        this.result = result;
    }

    protected isSwaggerException = true;

    static isSwaggerException(obj: any): obj is SwaggerException {
        return obj.isSwaggerException === true;
    }
}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any; }, result?: any): Observable<any> {
    if (result !== null && result !== undefined)
        return _observableThrow(result);
    else
        return _observableThrow(new SwaggerException(message, status, response, headers, null));
}

function blobToText(blob: any): Observable<string> {
    return new Observable<string>((observer: any) => {
        if (!blob) {
            observer.next("");
            observer.complete();
        } else {
            let reader = new FileReader();
            reader.onload = event => {
                observer.next((event.target as any).result);
                observer.complete();
            };
            reader.readAsText(blob);
        }
    });
}