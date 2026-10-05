describe('OfflineCPA E2E UI Automation', () => {
  it('Loads the Tax Dashboard and verifies UI elements', () => {
    cy.visit('/');
    cy.contains('Tax Preparation (1120-S)').should('be.visible');
    cy.contains('Upload File').should('be.visible');
    cy.contains('Export Book to Excel').should('be.visible');
  });

  it('Verifies the secure /user-assets file upload input accepts only offline documents', () => {
    cy.visit('/');
    cy.get('input[type="file"]')
      .should('exist')
      .should('have.attr', 'accept', '.pdf,.png,.jpg,.jpeg,.csv');
  });
});
