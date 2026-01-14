/**
 * Configuration mapping API categories to registration form pages
 */
export const registrationPageConfig = [
  {
    pageName: 'Personal Details',
    pageIndex: 0,
    categories: [
      'Contact Information',
      'Personal Information',
      'Professional Background',
      'Educational Background',
    ],
  },
  {
    pageName: 'Skills and Logistics',
    pageIndex: 1,
    categories: [
      'Skills & Interests',
      'Online Profiles',
      'Hackathon Preferences',
      'Dietary & Preferences',
    ],
  },
  {
    pageName: 'Outreach',
    pageIndex: 2,
    categories: [
      'Team Information',
      'Marketing & Outreach',
      'Career & Opportunities',
      'Communication Preferences',
    ],
  },
] as const

/**
 * Get categories for a specific page index
 */
export function getCategoriesForPage(pageIndex: number): readonly string[] {
  return registrationPageConfig[pageIndex]?.categories ?? []
}

/**
 * Get page name for a specific page index
 */
export function getPageName(pageIndex: number): string {
  return registrationPageConfig[pageIndex]?.pageName ?? ''
}

/**
 * Get total number of pages
 */
export function getTotalPages(): number {
  return registrationPageConfig.length
}
