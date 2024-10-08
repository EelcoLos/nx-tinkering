/* tslint:disable */
/* eslint-disable */
// Generated by Microsoft Kiota
// @ts-ignore
import {
  type Parsable,
  type ParseNode,
  type SerializationWriter,
} from '@microsoft/kiota-abstractions';

/**
 * Creates a new instance of the appropriate class based on discriminator value
 * @param parseNode The parse node to use to read the discriminator value and create the object
 * @returns {MyRequest}
 */
export function createMyRequestFromDiscriminatorValue(
  parseNode: ParseNode | undefined,
): (instance?: Parsable) => Record<string, (node: ParseNode) => void> {
  return deserializeIntoMyRequest;
}
/**
 * Creates a new instance of the appropriate class based on discriminator value
 * @param parseNode The parse node to use to read the discriminator value and create the object
 * @returns {MyResponse}
 */
export function createMyResponseFromDiscriminatorValue(
  parseNode: ParseNode | undefined,
): (instance?: Parsable) => Record<string, (node: ParseNode) => void> {
  return deserializeIntoMyResponse;
}
/**
 * The deserialization information for the current model
 * @returns {Record<string, (node: ParseNode) => void>}
 */
export function deserializeIntoMyRequest(
  myRequest: Partial<MyRequest> | undefined = {},
): Record<string, (node: ParseNode) => void> {
  return {
    age: (n) => {
      myRequest.age = n.getNumberValue();
    },
    firstName: (n) => {
      myRequest.firstName = n.getStringValue();
    },
    lastName: (n) => {
      myRequest.lastName = n.getStringValue();
    },
  };
}
/**
 * The deserialization information for the current model
 * @returns {Record<string, (node: ParseNode) => void>}
 */
export function deserializeIntoMyResponse(
  myResponse: Partial<MyResponse> | undefined = {},
): Record<string, (node: ParseNode) => void> {
  return {
    fullName: (n) => {
      myResponse.fullName = n.getStringValue();
    },
    isOver18: (n) => {
      myResponse.isOver18 = n.getBooleanValue();
    },
  };
}
export interface MyRequest extends Parsable {
  /**
   * The age property
   */
  age?: number;
  /**
   * The firstName property
   */
  firstName?: string;
  /**
   * The lastName property
   */
  lastName?: string;
}
export interface MyResponse extends Parsable {
  /**
   * The fullName property
   */
  fullName?: string;
  /**
   * The isOver18 property
   */
  isOver18?: boolean;
}
/**
 * Serializes information the current object
 * @param writer Serialization writer to use to serialize this model
 */
export function serializeMyRequest(
  writer: SerializationWriter,
  myRequest: Partial<MyRequest> | undefined = {},
): void {
  writer.writeNumberValue('age', myRequest.age);
  writer.writeStringValue('firstName', myRequest.firstName);
  writer.writeStringValue('lastName', myRequest.lastName);
}
/**
 * Serializes information the current object
 * @param writer Serialization writer to use to serialize this model
 */
export function serializeMyResponse(
  writer: SerializationWriter,
  myResponse: Partial<MyResponse> | undefined = {},
): void {
  writer.writeStringValue('fullName', myResponse.fullName);
  writer.writeBooleanValue('isOver18', myResponse.isOver18);
}
/* tslint:enable */
/* eslint-enable */
