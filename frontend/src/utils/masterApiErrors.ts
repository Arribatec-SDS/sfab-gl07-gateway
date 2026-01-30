/**
 * Utilities for handling Master API errors, specifically Keycloak configuration failures
 */

interface MasterApiError {
  message: string;
  endpoint?: string;
  isKeycloakConfigError: boolean;
  isNetworkError: boolean;
  statusCode?: number;
}

/**
 * Parse and categorize errors from Master API calls
 */
export function parseMasterApiError(error: any): MasterApiError {
  let message = 'Unknown error occurred';
  let isKeycloakConfigError = false;
  let isNetworkError = false;
  let statusCode: number | undefined;
  let endpoint: string | undefined;

  // Extract error message
  if (typeof error === 'string') {
    message = error;
  } else if (error?.message) {
    message = error.message;
  } else if (error?.response?.data?.message) {
    message = error.response.data.message;
  } else if (error?.response?.statusText) {
    message = error.response.statusText;
  }

  // Extract status code
  if (error?.response?.status) {
    statusCode = error.response.status;
  } else if (error?.status) {
    statusCode = error.status;
  }

  // Extract endpoint
  if (error?.config?.url) {
    endpoint = error.config.url;
  } else if (error?.url) {
    endpoint = error.url;
  }

  // Determine error type
  isKeycloakConfigError = 
    message.toLowerCase().includes('keycloak-config') ||
    message.toLowerCase().includes('keycloak') ||
    message.toLowerCase().includes('master') ||
    message.toLowerCase().includes('config') ||
    message.toLowerCase().includes('authority') ||
    message.toLowerCase().includes('realm') ||
    endpoint?.includes('keycloak-config') ||
    false;

  isNetworkError = 
    message.toLowerCase().includes('network') ||
    message.toLowerCase().includes('fetch') ||
    message.toLowerCase().includes('connection') ||
    message.toLowerCase().includes('timeout') ||
    message.toLowerCase().includes('cors') ||
    statusCode === 0 ||
    statusCode === 502 ||
    statusCode === 503 ||
    statusCode === 504 ||
    false;

  return {
    message,
    endpoint,
    isKeycloakConfigError,
    isNetworkError,
    statusCode
  };
}

/**
 * Generate user-friendly error message for Master API failures
 */
export function getMasterApiErrorMessage(error: MasterApiError): string {
  if (error.isKeycloakConfigError) {
    return `Failed to retrieve Keycloak configuration from Master API. ${
      error.isNetworkError 
        ? 'This appears to be a network connectivity issue.' 
        : 'The authentication service may be temporarily unavailable.'
    }`;
  }

  if (error.isNetworkError) {
    return 'Unable to connect to the Master API service. Please check your network connection.';
  }

  if (error.statusCode === 401) {
    return 'Authentication failed. Please check your credentials.';
  }

  if (error.statusCode === 403) {
    return 'Access denied. You do not have permission to access this resource.';
  }

  if (error.statusCode === 404) {
    return 'Master API service not found. The service may be down or misconfigured.';
  }

  if (error.statusCode && error.statusCode >= 500) {
    return 'Master API service is experiencing technical difficulties. Please try again later.';
  }

  return `Master API error: ${error.message}`;
}

/**
 * Generate technical details for debugging
 */
export function getMasterApiErrorDetails(error: MasterApiError): string {
  const details: string[] = [];
  
  if (error.endpoint) {
    details.push(`Endpoint: ${error.endpoint}`);
  }
  
  if (error.statusCode) {
    details.push(`Status Code: ${error.statusCode}`);
  }
  
  details.push(`Error: ${error.message}`);
  
  if (error.isKeycloakConfigError) {
    details.push('Error Type: Keycloak Configuration Failure');
    details.push('Expected Endpoint: /api/master/keycloak-config');
  }
  
  if (error.isNetworkError) {
    details.push('Error Type: Network/Connectivity Issue');
  }
  
  return details.join('\n');
}

/**
 * Check if error is related to the specific Keycloak config endpoint
 */
export function isKeycloakConfigEndpointError(error: any): boolean {
  const parsedError = parseMasterApiError(error);
  return parsedError.isKeycloakConfigError && 
    (parsedError.endpoint?.includes('keycloak-config') ||
     parsedError.message.toLowerCase().includes('keycloak-config'));
}

/**
 * Generate suggestions for resolving Master API errors
 */
export function getMasterApiErrorSuggestions(error: MasterApiError): string[] {
  const suggestions: string[] = [];

  if (error.isKeycloakConfigError) {
    // Put the most important suggestion first
    suggestions.push('Ensure the configuration of product and tenant is correct');
    suggestions.push('Verify the Master API service is running and accessible');
    suggestions.push('Check if the Keycloak service is running');
  }

  if (error.isNetworkError) {
    suggestions.push('Check your internet connection');
    suggestions.push('Verify the Master API URL is correct');
    suggestions.push('Check if there are any firewall or proxy issues');
  }

  if (error.statusCode === 404) {
    suggestions.push('Verify the Master API service is deployed');
    suggestions.push('Check the API endpoint configuration');
  }

  if (error.statusCode && error.statusCode >= 500) {
    suggestions.push('Contact your system administrator');
    suggestions.push('Check the Master API service logs');
    suggestions.push('Try again in a few minutes');
  }

  if (suggestions.length === 0) {
    suggestions.push('Try refreshing the page');
    suggestions.push('Contact your system administrator if the problem persists');
  }

  return suggestions;
}

/**
 * Generate admin app URL from current location
 * If current app is at <tenant>.<domain>/app, admin app will be at admin.<domain>/nexus-console
 */
export function getAdminAppUrl(): string {
  const currentHost = window.location.host;
  const protocol = window.location.protocol;
  
  // Extract domain from current host (e.g., demo.localtest.me:7364 -> localtest.me)
  const parts = currentHost.split('.');
  if (parts.length >= 2) {
    // Remove the tenant part and replace with 'admin'
    let domain = parts.slice(1).join('.');
    
    // Strip port number from domain (e.g., localtest.me:7364 -> localtest.me)
    domain = domain.split(':')[0];
    
    return `${protocol}//admin.${domain}/nexus-console`;
  }
  
  // Fallback if we can't parse the domain
  let fallbackDomain = currentHost.replace(/^[^.]+\./, '');
  // Strip port number from fallback domain too
  fallbackDomain = fallbackDomain.split(':')[0];
  
  return `${protocol}//admin.${fallbackDomain}/nexus-console`;
}