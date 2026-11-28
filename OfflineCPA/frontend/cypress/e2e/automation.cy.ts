describe('TrueBalance app shell', () => {
  it('opens to Home with a nav', () => {
    cy.visit('/');
    cy.get('nav').contains('Home').should('be.visible');
    cy.contains('Your screens').should('be.visible');
  });

  it('shows the not-found page for an unknown address', () => {
    cy.visit('/#/nowhere');
    cy.contains('Page not found').should('be.visible');
  });
});
