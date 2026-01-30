import { createTheme, alpha } from '@mui/material/styles';

// Arribatec Brand Colors (from official branding guidelines)
export const arribatecColors = {
  rustyRed: '#A95F55',      // Primary accent, symbols, badges
  darkBlue: '#18272F',      // Text, backgrounds, headers
  oceanBlue: '#5E7898',     // Secondary accent
  gold: '#C2AC6F',          // Highlights, premium elements
  almostWhite: '#EEECE7',   // Background color
  almostBlack: '#323232',   // Logo, text
  white: '#FFFFFF',
  
  // Extended palette for UI states
  success: '#4CAF50',
  error: '#D32F2F',
  warning: '#ED6C02',
  info: '#5E7898',          // Using Ocean Blue for info
};

// Create the Arribatec MUI Theme
const arribatecTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: arribatecColors.rustyRed,
      light: alpha(arribatecColors.rustyRed, 0.7),
      dark: '#8B4D45',
      contrastText: arribatecColors.white,
    },
    secondary: {
      main: arribatecColors.oceanBlue,
      light: alpha(arribatecColors.oceanBlue, 0.7),
      dark: '#4A5F78',
      contrastText: arribatecColors.white,
    },
    background: {
      default: arribatecColors.almostWhite,
      paper: arribatecColors.white,
    },
    text: {
      primary: arribatecColors.darkBlue,
      secondary: arribatecColors.almostBlack,
    },
    success: {
      main: arribatecColors.success,
    },
    error: {
      main: arribatecColors.error,
    },
    warning: {
      main: arribatecColors.warning,
    },
    info: {
      main: arribatecColors.oceanBlue,
    },
    divider: alpha(arribatecColors.darkBlue, 0.12),
  },
  
  typography: {
    fontFamily: '"Inter", system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    
    h1: {
      fontSize: '2.5rem',
      fontWeight: 700,
      color: arribatecColors.darkBlue,
      letterSpacing: '-0.02em',
    },
    h2: {
      fontSize: '2rem',
      fontWeight: 700,
      color: arribatecColors.darkBlue,
      letterSpacing: '-0.01em',
    },
    h3: {
      fontSize: '1.75rem',
      fontWeight: 600,
      color: arribatecColors.darkBlue,
    },
    h4: {
      fontSize: '1.5rem',
      fontWeight: 600,
      color: arribatecColors.darkBlue,
    },
    h5: {
      fontSize: '1.25rem',
      fontWeight: 600,
      color: arribatecColors.darkBlue,
    },
    h6: {
      fontSize: '1rem',
      fontWeight: 600,
      color: arribatecColors.darkBlue,
    },
    body1: {
      fontSize: '1rem',
      lineHeight: 1.6,
      color: arribatecColors.almostBlack,
    },
    body2: {
      fontSize: '0.875rem',
      lineHeight: 1.5,
      color: arribatecColors.almostBlack,
    },
    button: {
      fontWeight: 600,
      textTransform: 'none' as const,
      letterSpacing: '0.02em',
    },
    caption: {
      fontSize: '0.75rem',
      color: alpha(arribatecColors.almostBlack, 0.7),
    },
  },
  
  shape: {
    borderRadius: 8,
  },
  
  shadows: [
    'none',
    '0px 2px 4px rgba(24, 39, 47, 0.05)',
    '0px 4px 8px rgba(24, 39, 47, 0.08)',
    '0px 8px 16px rgba(24, 39, 47, 0.1)',
    '0px 12px 24px rgba(24, 39, 47, 0.12)',
    '0px 16px 32px rgba(24, 39, 47, 0.14)',
    '0px 20px 40px rgba(24, 39, 47, 0.16)',
    '0px 24px 48px rgba(24, 39, 47, 0.18)',
    '0px 28px 56px rgba(24, 39, 47, 0.2)',
    '0px 32px 64px rgba(24, 39, 47, 0.22)',
    '0px 36px 72px rgba(24, 39, 47, 0.24)',
    '0px 40px 80px rgba(24, 39, 47, 0.26)',
    '0px 44px 88px rgba(24, 39, 47, 0.28)',
    '0px 48px 96px rgba(24, 39, 47, 0.3)',
    '0px 52px 104px rgba(24, 39, 47, 0.32)',
    '0px 56px 112px rgba(24, 39, 47, 0.34)',
    '0px 60px 120px rgba(24, 39, 47, 0.36)',
    '0px 64px 128px rgba(24, 39, 47, 0.38)',
    '0px 68px 136px rgba(24, 39, 47, 0.4)',
    '0px 72px 144px rgba(24, 39, 47, 0.42)',
    '0px 76px 152px rgba(24, 39, 47, 0.44)',
    '0px 80px 160px rgba(24, 39, 47, 0.46)',
    '0px 84px 168px rgba(24, 39, 47, 0.48)',
    '0px 88px 176px rgba(24, 39, 47, 0.5)',
    '0px 92px 184px rgba(24, 39, 47, 0.52)',
  ],
  
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: arribatecColors.almostWhite,
          color: arribatecColors.darkBlue,
        },
      },
    },
    
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          padding: '10px 24px',
          fontSize: '0.9375rem',
          fontWeight: 600,
          boxShadow: 'none',
          '&:hover': {
            boxShadow: '0px 4px 12px rgba(169, 95, 85, 0.25)',
          },
        },
        contained: {
          '&:hover': {
            boxShadow: '0px 6px 16px rgba(169, 95, 85, 0.3)',
          },
        },
        containedPrimary: {
          background: `linear-gradient(135deg, ${arribatecColors.rustyRed} 0%, #8B4D45 100%)`,
          '&:hover': {
            background: `linear-gradient(135deg, #8B4D45 0%, ${arribatecColors.rustyRed} 100%)`,
          },
        },
        containedSecondary: {
          background: `linear-gradient(135deg, ${arribatecColors.oceanBlue} 0%, #4A5F78 100%)`,
          '&:hover': {
            background: `linear-gradient(135deg, #4A5F78 0%, ${arribatecColors.oceanBlue} 100%)`,
          },
        },
        outlined: {
          borderWidth: 2,
          '&:hover': {
            borderWidth: 2,
            backgroundColor: alpha(arribatecColors.rustyRed, 0.04),
          },
        },
        outlinedPrimary: {
          borderColor: arribatecColors.rustyRed,
          color: arribatecColors.rustyRed,
        },
        text: {
          '&:hover': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.08),
          },
        },
      },
    },
    
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: arribatecColors.darkBlue,
          boxShadow: '0px 2px 8px rgba(24, 39, 47, 0.15)',
        },
      },
    },
    
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: '0px 4px 20px rgba(24, 39, 47, 0.08)',
          border: `1px solid ${alpha(arribatecColors.darkBlue, 0.08)}`,
          transition: 'transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out',
          '&:hover': {
            transform: 'translateY(-2px)',
            boxShadow: '0px 8px 30px rgba(24, 39, 47, 0.12)',
          },
        },
      },
    },
    
    MuiCardContent: {
      styleOverrides: {
        root: {
          padding: '24px',
          '&:last-child': {
            paddingBottom: '24px',
          },
        },
      },
    },
    
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 500,
          borderRadius: 6,
        },
        filled: {
          '&.MuiChip-colorSuccess': {
            backgroundColor: alpha(arribatecColors.success, 0.15),
            color: arribatecColors.success,
          },
          '&.MuiChip-colorError': {
            backgroundColor: alpha(arribatecColors.error, 0.15),
            color: arribatecColors.error,
          },
        },
        outlined: {
          borderWidth: 1.5,
        },
      },
    },
    
    MuiTableHead: {
      styleOverrides: {
        root: {
          '& .MuiTableCell-head': {
            backgroundColor: arribatecColors.darkBlue,
            color: arribatecColors.white,
            fontWeight: 600,
            fontSize: '0.875rem',
          },
        },
      },
    },
    
    MuiTableRow: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.04),
          },
          '&:nth-of-type(even)': {
            backgroundColor: alpha(arribatecColors.almostWhite, 0.5),
          },
        },
      },
    },
    
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderBottom: `1px solid ${alpha(arribatecColors.darkBlue, 0.08)}`,
          padding: '16px',
        },
      },
    },
    
    MuiAvatar: {
      styleOverrides: {
        root: {
          backgroundColor: arribatecColors.rustyRed,
          color: arribatecColors.white,
          fontWeight: 600,
        },
      },
    },
    
    MuiTabs: {
      styleOverrides: {
        indicator: {
          height: 3,
          borderRadius: '3px 3px 0 0',
          backgroundColor: arribatecColors.rustyRed,
        },
      },
    },
    
    MuiTab: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          fontSize: '0.9375rem',
          textTransform: 'none',
          minHeight: 56,
          '&.Mui-selected': {
            color: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    MuiAlert: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
        standardSuccess: {
          backgroundColor: alpha(arribatecColors.success, 0.1),
          color: arribatecColors.success,
        },
        standardError: {
          backgroundColor: alpha(arribatecColors.error, 0.1),
          color: arribatecColors.error,
        },
        standardWarning: {
          backgroundColor: alpha(arribatecColors.warning, 0.1),
          color: arribatecColors.warning,
        },
        standardInfo: {
          backgroundColor: alpha(arribatecColors.oceanBlue, 0.1),
          color: arribatecColors.oceanBlue,
        },
      },
    },
    
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
        },
        rounded: {
          borderRadius: 12,
        },
      },
    },
    
    MuiToolbar: {
      styleOverrides: {
        root: {
          minHeight: '64px !important',
        },
      },
    },
    
    MuiCircularProgress: {
      styleOverrides: {
        root: {
          color: arribatecColors.rustyRed,
        },
      },
    },
    
    MuiLinearProgress: {
      styleOverrides: {
        root: {
          borderRadius: 4,
          backgroundColor: alpha(arribatecColors.rustyRed, 0.15),
        },
        bar: {
          borderRadius: 4,
          backgroundColor: arribatecColors.rustyRed,
        },
      },
    },
    
    MuiDivider: {
      styleOverrides: {
        root: {
          borderColor: alpha(arribatecColors.darkBlue, 0.1),
        },
      },
    },
    
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 8,
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: arribatecColors.rustyRed,
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: arribatecColors.rustyRed,
              borderWidth: 2,
            },
          },
        },
      },
    },
    
    // Form Controls - prevent default MUI blue
    MuiCheckbox: {
      styleOverrides: {
        root: {
          color: alpha(arribatecColors.darkBlue, 0.6),
          '&.Mui-checked': {
            color: arribatecColors.rustyRed,
          },
          '&.Mui-disabled': {
            color: alpha(arribatecColors.darkBlue, 0.3),
          },
        },
      },
    },
    
    MuiRadio: {
      styleOverrides: {
        root: {
          color: alpha(arribatecColors.darkBlue, 0.6),
          '&.Mui-checked': {
            color: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    MuiSwitch: {
      styleOverrides: {
        switchBase: {
          '&.Mui-checked': {
            color: arribatecColors.rustyRed,
            '& + .MuiSwitch-track': {
              backgroundColor: arribatecColors.rustyRed,
              opacity: 0.7,
            },
          },
        },
        track: {
          backgroundColor: alpha(arribatecColors.darkBlue, 0.3),
        },
      },
    },
    
    MuiSlider: {
      styleOverrides: {
        root: {
          color: arribatecColors.rustyRed,
        },
        thumb: {
          '&:hover, &.Mui-focusVisible': {
            boxShadow: `0px 0px 0px 8px ${alpha(arribatecColors.rustyRed, 0.16)}`,
          },
        },
        valueLabel: {
          backgroundColor: arribatecColors.darkBlue,
        },
      },
    },
    
    MuiSelect: {
      styleOverrides: {
        root: {
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    MuiFormLabel: {
      styleOverrides: {
        root: {
          '&.Mui-focused': {
            color: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    MuiInputLabel: {
      styleOverrides: {
        root: {
          '&.Mui-focused': {
            color: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    MuiFilledInput: {
      styleOverrides: {
        root: {
          '&:after': {
            borderBottomColor: arribatecColors.rustyRed,
          },
          '&.Mui-focused:after': {
            borderBottomColor: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    // Interactive Components
    MuiLink: {
      styleOverrides: {
        root: {
          color: arribatecColors.rustyRed,
          textDecorationColor: alpha(arribatecColors.rustyRed, 0.4),
          '&:hover': {
            color: '#8B4D45',
          },
        },
      },
    },
    
    MuiIconButton: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.08),
          },
        },
        colorPrimary: {
          color: arribatecColors.rustyRed,
        },
      },
    },
    
    MuiBadge: {
      styleOverrides: {
        colorPrimary: {
          backgroundColor: arribatecColors.rustyRed,
          color: arribatecColors.white,
        },
        colorSecondary: {
          backgroundColor: arribatecColors.oceanBlue,
          color: arribatecColors.white,
        },
      },
    },
    
    MuiFab: {
      styleOverrides: {
        root: {
          boxShadow: '0px 4px 12px rgba(169, 95, 85, 0.25)',
        },
        primary: {
          backgroundColor: arribatecColors.rustyRed,
          '&:hover': {
            backgroundColor: '#8B4D45',
          },
        },
        secondary: {
          backgroundColor: arribatecColors.oceanBlue,
          '&:hover': {
            backgroundColor: '#4A5F78',
          },
        },
      },
    },
    
    MuiTooltip: {
      styleOverrides: {
        tooltip: {
          backgroundColor: arribatecColors.darkBlue,
          color: arribatecColors.white,
          fontSize: '0.75rem',
          fontWeight: 500,
        },
        arrow: {
          color: arribatecColors.darkBlue,
        },
      },
    },
    
    // Menu & List Components
    MuiMenuItem: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.08),
          },
          '&.Mui-selected': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.12),
            '&:hover': {
              backgroundColor: alpha(arribatecColors.rustyRed, 0.16),
            },
          },
        },
      },
    },
    
    MuiListItemButton: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.08),
          },
          '&.Mui-selected': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.12),
            borderLeft: `3px solid ${arribatecColors.rustyRed}`,
            '&:hover': {
              backgroundColor: alpha(arribatecColors.rustyRed, 0.16),
            },
          },
        },
      },
    },
    
    MuiListItemIcon: {
      styleOverrides: {
        root: {
          color: arribatecColors.darkBlue,
          minWidth: 40,
        },
      },
    },
    
    // Pagination
    MuiPaginationItem: {
      styleOverrides: {
        root: {
          '&.Mui-selected': {
            backgroundColor: arribatecColors.rustyRed,
            color: arribatecColors.white,
            '&:hover': {
              backgroundColor: '#8B4D45',
            },
          },
        },
      },
    },
    
    // Stepper
    MuiStepIcon: {
      styleOverrides: {
        root: {
          color: alpha(arribatecColors.darkBlue, 0.3),
          '&.Mui-active': {
            color: arribatecColors.rustyRed,
          },
          '&.Mui-completed': {
            color: arribatecColors.success,
          },
        },
      },
    },
    
    MuiStepLabel: {
      styleOverrides: {
        label: {
          '&.Mui-active': {
            color: arribatecColors.rustyRed,
            fontWeight: 600,
          },
        },
      },
    },
    
    // Dialog
    MuiDialogTitle: {
      styleOverrides: {
        root: {
          fontSize: '1.25rem',
          fontWeight: 600,
          color: arribatecColors.darkBlue,
        },
      },
    },
    
    MuiBackdrop: {
      styleOverrides: {
        root: {
          backgroundColor: alpha(arribatecColors.darkBlue, 0.5),
        },
      },
    },
    
    // Toggle Buttons
    MuiToggleButton: {
      styleOverrides: {
        root: {
          '&.Mui-selected': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.15),
            color: arribatecColors.rustyRed,
            '&:hover': {
              backgroundColor: alpha(arribatecColors.rustyRed, 0.25),
            },
          },
        },
      },
    },
    
    // Accordion
    MuiAccordionSummary: {
      styleOverrides: {
        root: {
          '&.Mui-expanded': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.04),
          },
        },
        expandIconWrapper: {
          color: arribatecColors.rustyRed,
        },
      },
    },
    
    // Bottom Navigation
    MuiBottomNavigationAction: {
      styleOverrides: {
        root: {
          '&.Mui-selected': {
            color: arribatecColors.rustyRed,
          },
        },
      },
    },
    
    // Rating
    MuiRating: {
      styleOverrides: {
        iconFilled: {
          color: arribatecColors.gold,
        },
        iconHover: {
          color: arribatecColors.gold,
        },
      },
    },
    
    // Breadcrumbs
    MuiBreadcrumbs: {
      styleOverrides: {
        separator: {
          color: alpha(arribatecColors.darkBlue, 0.5),
        },
      },
    },
    
    // Speed Dial
    MuiSpeedDialAction: {
      styleOverrides: {
        fab: {
          backgroundColor: arribatecColors.white,
          color: arribatecColors.rustyRed,
          '&:hover': {
            backgroundColor: alpha(arribatecColors.rustyRed, 0.08),
          },
        },
      },
    },
  },
});

export default arribatecTheme;

// Export individual color for use in custom styling
export const { rustyRed, darkBlue, oceanBlue, gold, almostWhite, almostBlack } = arribatecColors;
