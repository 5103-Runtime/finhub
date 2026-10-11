import z from "zod";

/**
 * The maximum age in years for a user.
 * This is used to validate the date of birth field in the profile form.
 */
export const maximumAgeYears = 120;

/**
 * @brief Formats a Date object into a string in the format "YYYY-MM-DD".
 * @param date The date to format.
 * @returns The formatted date string.
 */
function formatDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

const today = new Date();

/**
 * The latest date of birth accepted by the profile form.
 */
export const maximumDateOfBirth = formatDateInputValue(today);
const minimumDate = new Date(today);
minimumDate.setFullYear(minimumDate.getFullYear() - maximumAgeYears);

/**
 * The earliest date of birth accepted by the profile form.
 */
export const minimumDateOfBirth = formatDateInputValue(minimumDate);

/**
 * Validates profile form values, including required names and a date of birth
 * within the configured age range.
 */
export const profileSchema = z.object({
  firstName: z
    .string()
    .min(1, "First name is required")
    .max(50, "Must be 50 characters or less"),
  lastName: z
    .string()
    .min(1, "Last name is required")
    .max(50, "Must be 50 characters or less"),
  preferredName: z.string().max(50, "Must be 50 characters or less").optional(),
  dateOfBirth: z
    .string()
    .min(1, "Date of birth is required")
    .refine((value) => value <= maximumDateOfBirth, {
      message: "Date of birth cannot be in the future",
    })
    .refine((value) => value >= minimumDateOfBirth, {
      message: `Date of birth cannot be more than ${maximumAgeYears} years ago`,
    }),
});

/**
 * Represents the validated values submitted by a profile form.
 */
export type ProfileFormData = z.infer<typeof profileSchema>;
