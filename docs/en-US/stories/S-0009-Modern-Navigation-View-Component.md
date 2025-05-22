# S-0009-Modern-Navigation-View-Component

<!-- Priority: P1 -->
<!-- Status: To Start -->

## Overview

Design and implement a modern two-column layout component called **NavigationView** for the Modulus desktop application. This component will provide a consistent and flexible application navigation structure with a collapsible sidebar and dynamic content area.

## Goals

1. Create a responsive two-column grid layout: left fixed width, right fluid
2. Implement a full NavigationBar sectioned into Header/Body/Footer
3. Support page injection and routing functionality
4. Provide sidebar collapse/expand functionality

## Layout Structure

```
<NavigationView Height="100%" Width="100%">
┌───────────────────────┬───────────────────────────────────────────────────────────────┐
│ ▌ Left Panel          │ ▌ Right Panel                                                 │
│   NavigationBar       │   ContentArea (active page)                                   │
│ ┌───────────────────┐ │ ┌───────────────────────────────────────────────────────────┐ │
│ │ ▼ Header (140px)  │ │ │ - Page content container                                  │ │
│ │   App icon (32px) │ │ │ - Shows selected NavigationItem page (default: Dashboard) │ │
│ │   App name + ver  │ │ │ - Includes scroll support if needed                       │ │
│ │   CollapseButton  │ │ │ - Automatically resizes with window                       │ │
│ ├───────────────────┤ │ └───────────────────────────────────────────────────────────┘ │
│ │ ▼ Body (flexible) │ │                                                               │
│ │   NavigationItems │ │                                                               │
│ │   (Dashboard, …)  │ │                                                               │
│ ├───────────────────┤ │                                                               │
│ │ ▼ Footer (auto)   │ │                                                               │
│ │   Settings, etc.  │ │                                                               │
│ └───────────────────┘ └───────────────────────────────────────────────────────────────┘
</NavigationView>
```

## Sizing Rules

- Root: 2-column Grid
  • Column 0 (NavigationBar): Width = 220px (expanded) or 72px (collapsed)
  • Column 1 (ContentArea): Width = * (fills remaining width)

- Header: 140px height
- NavigationItems: 40px height each, 14px text, 24px icon
- Footer: Auto-height, pinned bottom
- Sidebar color: #1E1E2E (dark), content background: #F9FAFB

## Component Architecture

```
<NavigationView>
 ├── <NavigationBar>
 │    ├── Header (logo, title, version, collapse button)
 │    ├── Scrollable Menu Items (body)
 │    └── Footer (settings, permissions, feedback)
 └── <ContentArea>
      └── Frame/ContentControl displaying selected page
```

## Interactions

- Collapse/Expand: CollapseButton toggles NavigationBar width and fades labels
- NavigationItem click: triggers INavigationService.Navigate(route)
- Pages load in right ContentArea with transitions
- Keyboard: Ctrl+Tab cycles, Esc collapses nav bar

## Acceptance Criteria

- [x] Create Story and add to project board
- [ ] Design and implement NavigationView component with Avalonia UI
- [ ] Implement NavigationBar component with Header/Body/Footer sections
- [ ] Implement ContentArea component with page switching and transitions
- [ ] Add collapse/expand functionality with animations
- [ ] Create sample pages to demonstrate NavigationView usage
- [ ] Write unit tests to ensure the component works correctly
- [ ] Create developer documentation on how to use the component 