/* tslint:disable */
/* eslint-disable */
// Generated by Microsoft Kiota
// @ts-ignore
import {
  createMyResponseFromDiscriminatorValue,
  serializeMyRequest,
  serializeMyResponse,
  type MyRequest,
  type MyResponse,
} from '../../../models/index.js';
// @ts-ignore
import {
  ModelSerializerFunction,
  type BaseRequestBuilder,
  type Parsable,
  type ParsableFactory,
  type RequestConfiguration,
  type RequestInformation,
  type RequestsMetadata,
} from '@microsoft/kiota-abstractions';

/**
 * Builds and executes requests for operations under /api/user/create
 */
export interface CreateRequestBuilder
  extends BaseRequestBuilder<CreateRequestBuilder> {
  /**
   * @param body The request body
   * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
   * @returns {Promise<MyResponse>}
   */
  post(
    body: MyRequest,
    requestConfiguration?: RequestConfiguration<object> | undefined,
  ): Promise<MyResponse | undefined>;
  /**
   * @param body The request body
   * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
   * @returns {RequestInformation}
   */
  toPostRequestInformation(
    body: MyRequest,
    requestConfiguration?: RequestConfiguration<object> | undefined,
  ): RequestInformation;
}
/**
 * Uri template for the request builder.
 */
export const CreateRequestBuilderUriTemplate = '{+baseurl}/api/user/create';
/**
 * Metadata for all the requests in the request builder.
 */
export const CreateRequestBuilderRequestsMetadata: RequestsMetadata = {
  post: {
    uriTemplate: CreateRequestBuilderUriTemplate,
    responseBodyContentType: 'application/json',
    adapterMethodName: 'send',
    responseBodyFactory: createMyResponseFromDiscriminatorValue,
    requestBodyContentType: 'application/json',
    requestBodySerializer:
      serializeMyRequest as ModelSerializerFunction<Parsable>,
    requestInformationContentSetMethod: 'setContentFromParsable',
  },
};
/* tslint:enable */
/* eslint-enable */
