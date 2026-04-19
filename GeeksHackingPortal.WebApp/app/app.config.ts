export default defineAppConfig({
  ui: {
    colors: {
      primary: 'orange',
      neutral: 'stone',
    },
    card: {
      slots: {
        root: 'rounded-xl overflow-hidden',
        header: 'px-4 py-3 sm:px-4 sm:py-3.5',
        body: 'p-4 sm:p-4',
        footer: 'px-4 py-3 sm:px-4 sm:py-3.5',
      },
    },
    dashboardPanel: {
      slots: {
        body: 'flex flex-col gap-3 sm:gap-4 flex-1 overflow-y-auto p-3 sm:p-4',
      },
    },
    dashboardNavbar: {
      slots: {
        root: 'h-(--ui-header-height) shrink-0 flex items-center justify-between border-b border-default px-3 sm:px-4 gap-1.5',
      },
    },
  },
})
