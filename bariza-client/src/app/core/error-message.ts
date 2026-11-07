import { TranslateService } from '@ngx-translate/core';

import { ApiError } from './interceptors/api-error.interceptor';

// Exact-match backend error messages to frontend translation keys.
const backendErrorToKey: Record<string, string> = {
  // Country
  "The country cannot be deleted while it has cities or warehouses.": "errors.countryInUse",
  "The requested country was not found.": "errors.countryNotFound",
  // City
  "The city cannot be deleted while it has warehouses.": "errors.cityInUse",
  "The requested city was not found.": "errors.cityNotFound",
  // User
  "The requested user was not found.": "errors.userNotFound",
  "Your account is disabled and you need to contact support.": "errors.accountDisabled",
  "The default administrator cannot be deleted.": "errors.defaultAdminCannotBeDeleted",
  "Administrator accounts cannot be deleted.": "errors.administratorAccountsCannotBeDeleted",
  "You cannot deactivate your own account.": "errors.cannotDeactivateOwnAccount",
  "You cannot remove your own administrator role.": "errors.cannotRemoveOwnAdminRole",
  "You cannot delete your own account.": "errors.cannotDeleteOwnAccount",
  "Password must be at least 8 characters long.": "errors.passwordTooShort",
  "Current password is incorrect.": "errors.currentPasswordIncorrect",
  "New password must be different from the current password.":
    "errors.newPasswordMustDiffer",
  "You must change your password before accessing other endpoints.":
    "errors.passwordChangeRequired",
  // Warehouse
  "The requested warehouse was not found.": "errors.warehouseNotFound",
  // Warehouse item
  "The requested warehouse item was not found.": "errors.warehouseItemNotFound",
  // Inventory update
  "An inventory update request is required.": "errors.inventoryUpdateRequestRequired",
  "WarehouseId must be greater than zero.": "errors.warehouseIdMustBePositive",
  "SKUCode is required.": "errors.skuCodeRequired",
  "Quantity must be greater than zero.": "errors.quantityMustBePositive",
  // System logs
  "The requested log entry was not found.": "errors.logNotFound",
  "The start date must be before the end date.": "errors.invalidDateRange",
  "Retention days must be between 1 and 3650.": "errors.invalidRetention",
  "Level must be one of: Verbose, Debug, Information, Warning, Error, Fatal.":
    "errors.invalidLogLevel",
};

// Prefix patterns (parameterized backend messages) to translation keys.
// The backend already substitutes values, so translations receive the original
// English message as the `message` parameter for display fallback.
const backendErrorPrefixToKey: Array<[string, string]> = [
  ["Username '", "errors.usernameTaken"],
  ["Insufficient stock for SKU '", "errors.insufficientStock"],
  ["Multiple warehouse items match the supplied SKU", "errors.skuAmbiguous"],
];

export function errorMessage(error: unknown, translate: TranslateService): string {
  let message: string | undefined;

  if (error instanceof ApiError) {
    message = error.message;
  } else if (error instanceof Error && error.message) {
    message = error.message;
  }

  if (!message) {
    return translate.instant('errors.generic');
  }

  // Exact match first.
  const key = backendErrorToKey[message];
  if (key) {
    return translate.instant(key);
  }

  // Prefix match for parameterized messages.
  for (const [prefix, translationKey] of backendErrorPrefixToKey) {
    if (message.startsWith(prefix)) {
      // Pass the original English message as a fallback parameter.
      return translate.instant(translationKey, { message });
    }
  }

  return message;
}
