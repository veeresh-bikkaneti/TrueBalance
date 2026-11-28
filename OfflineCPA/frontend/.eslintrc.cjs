module.exports = {
  root: true,
  env: { browser: true, es2020: true },
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react-hooks/recommended',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs'],
  parser: '@typescript-eslint/parser',
  plugins: ['react-refresh'],
  rules: {
    'react-refresh/only-export-components': [
      'warn',
      { allowConstantExport: true },
    ],
  },
  overrides: [
    // typescript-eslint 8 reshuffled its recommended preset. This keeps our rule set
    // where it was. It sits in an override because the preset uses overrides too.
    {
      files: ['**/*.{ts,tsx}'],
      rules: {
        'no-class-assign': 'error',
        'no-with': 'error',
        '@typescript-eslint/no-unused-expressions': 'off',
        '@typescript-eslint/prefer-namespace-keyword': 'off',
      },
    },
    // Import boundaries, so screens can be built side by side without tangling.
    {
      files: ['src/features/**/*.{ts,tsx}'],
      rules: {
        'no-restricted-imports': ['error', {
          patterns: [{
            // From a feature, the only way up is to ../../shared.
            group: ['../*', '!../../', '../../*', '!../../shared', '**/features', '**/shell'],
            message: 'A feature imports its own files (./) and ../../shared only.',
          }],
        }],
      },
    },
    {
      files: ['src/shared/**/*.{ts,tsx}'],
      rules: {
        'no-restricted-imports': ['error', {
          patterns: [{
            group: ['**/shell', '**/features'],
            message: 'shared/ must not import shell/ or features/.',
          }],
        }],
      },
    },
  ],
}
