/** Shows a new recovery code until the owner says it is on paper. */
export function CodeOnce({ code, onWritten }: { code: string; onWritten: () => void }) {
  return (
    <section className="vault-form" aria-label="Your recovery code">
      <h3>Your recovery code</h3>
      <p>Write it on paper now. It is shown only once, so leaving this screen loses it too.</p>
      <p className="vault-code">{code}</p>
      <label className="vault-tick">
        <input type="checkbox" checked={false} onChange={onWritten} />
        I wrote it down (this hides the code)
      </label>
    </section>
  );
}
