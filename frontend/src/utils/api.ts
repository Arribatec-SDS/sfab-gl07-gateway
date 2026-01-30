/**
 * API utility functions for building API paths dynamically
 */

import axios, { AxiosError, AxiosInstance } from 'axios';
import { parseMasterApiError, getMasterApiErrorMessage } from './masterApiErrors';

/**
 * Gets the base path from window.location.pathname
 * Extracts the first path segment as the app base path
 * 
 * For example:
 * - URL: https://admin.localtest.me/sample/some/page
 * - Returns: "/sample"
 */
export const getBasePath = (): string => {
  const pathname = window.location.pathname;
  
  // Extract the first path segment as the base path
  // e.g., "/sample/some/page" -> "/sample"
  const segments = pathname.split('/').filter(Boolean);
  
  if (segments.length > 0) {
    return `/${segments[0]}`;
  }
  
  // Fallback to Vite config if no path segment found
  const basePath = import.meta.env.BASE_URL;
  return basePath.endsWith('/') ? basePath.slice(0, -1) : basePath;
};

/**
 * Builds an API path relative to the app's base path
 * 
 * For example:
 * - If base path is "/sample" and endpoint is "/user"
 * - Returns: "/sample/api/user"
 * 
 * @param endpoint - The API endpoint (e.g., "/user", "tenants", etc.)
 * @returns Full API path
 */
export const buildApiPath = (endpoint: string): string => {
  const basePath = getBasePath();
  
  // Ensure endpoint starts with /
  const normalizedEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  
  // Build the full API path: {basePath}/api{endpoint}
  return `${basePath}/api${normalizedEndpoint}`;
};

/**
 * Gets the full API base URL using window.location
 * Constructs the URL from: origin + pathname prefix (app base path) + /api
 * 
 * @returns The full API base URL (e.g., "https://admin.localtest.me/sample/api")
 */
export const getApiBasePath = (): string => {
  const basePath = getBasePath();
  const fullUrl = `${window.location.origin}${basePath}/api`;
  return fullUrl;
};

/**
 * Enhanced error handler for API calls
 */
const handleApiError = (error: AxiosError) => {
  console.error('API Error:', error);
  
  // Parse the error using our Master API error utilities
  const parsedError = parseMasterApiError(error);
  const userMessage = getMasterApiErrorMessage(parsedError);
  
  // Check if this is a critical Master API error that should trigger UI error handling
  if (parsedError.isKeycloakConfigError || parsedError.isNetworkError) {
    // Dispatch a custom event that can be caught by error handlers
    const errorEvent = new CustomEvent('masterApiError', {
      detail: {
        originalError: error,
        parsedError,
        userMessage
      }
    });
    window.dispatchEvent(errorEvent);
  }
  
  // Re-throw the error so it can be handled by calling code
  throw error;
};

/**
 * Creates a pre-configured axios instance with the correct base URL and error handling
 * This is the recommended way to make API calls in the application
 * 
 * @returns Configured axios instance
 */
export const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: getApiBasePath(),
    timeout: 30000,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Add response interceptor for error handling
  client.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => handleApiError(error)
  );

  return client;
};

/**
 * Create a specific client for Master API calls with enhanced error handling
 */
export const createMasterApiClient = (): AxiosInstance => {
  const basePath = getBasePath();
  const client = axios.create({
    baseURL: `${basePath}/api/master`,
    timeout: 30000,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // Add response interceptor specifically for Master API errors
  client.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      console.error('Master API Error:', error);
      
      // Always parse Master API errors
      const parsedError = parseMasterApiError(error);
      const userMessage = getMasterApiErrorMessage(parsedError);
      
      // Dispatch error event for UI handling
      const errorEvent = new CustomEvent('masterApiError', {
        detail: {
          originalError: error,
          parsedError,
          userMessage,
          isMasterApi: true
        }
      });
      window.dispatchEvent(errorEvent);
      
      throw error;
    }
  );

  return client;
};

/**
 * Example usage:
 * 
 * // Simple usage with buildApiPath
 * const userEndpoint = buildApiPath('/user');
 * axios.get(userEndpoint);
 * 
 * // Recommended: Use axios instance
 * const apiClient = createApiClient();
 * 
 * // Add auth token to requests
 * const token = await getToken();
 * apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
 * 
 * // Make API calls - base path is automatically handled
 * apiClient.get('/user');          // -> GET /sample/api/user
 * apiClient.post('/items', data);  // -> POST /sample/api/items
 */

