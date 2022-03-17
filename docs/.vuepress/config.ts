import { defineUserConfig } from 'vuepress'
import type { DefaultThemeOptions } from 'vuepress'

export default defineUserConfig<DefaultThemeOptions>({
  // site config
  lang: 'en-US',
  title: 'CensorCore',
  description: 'A flexible framework for censoring NSFW images',
  base: '/censor-core/',
  // theme and its config
  theme: '@vuepress/theme-default',
  themeConfig: {
    logo: '/siteIcon.png',
    repo: 'silveredgold/censor-core',
    docsDir: 'docs',
    navbar: [
        // NavbarItem
        {
          text: 'Introduction',
          link: '/',
        },
        // NavbarGroup
        {
          text: 'Usage',
            link: '/usage'
        },
        // string - page file path
        {
            text: 'Components',
            children: ['/projects', '/ai-components', '/censoring-components']
        },
      ],
  },
})