export enum EndpointType {
  External = "external",
  Internal = "internal",
}

export type BrowserStorageTarget = {
  endpointType: EndpointType;
  signedHost?: string;
  url: string;
};

const INTERNAL_STORAGE_HOSTS = new Set(["minio", "minio-magomedali-tech"]);

export const getEndpointType = (storageUrl: URL): EndpointType =>
  INTERNAL_STORAGE_HOSTS.has(storageUrl.hostname)
    ? EndpointType.Internal
    : EndpointType.External;

export const resolveBrowserStorageTarget = (
  storageUrl: string,
): BrowserStorageTarget => {
  const url = new URL(storageUrl);
  const endpointType = getEndpointType(url);

  if (endpointType === EndpointType.External) {
    return { endpointType, url: storageUrl };
  }

  return {
    endpointType,
    url: `/storage${url.pathname}${url.search}`,
    signedHost: url.host,
  };
};

export const getBrowserStorageUrl = (storageUrl: string): string =>
  resolveBrowserStorageTarget(storageUrl).url;
