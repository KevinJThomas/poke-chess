export default function Header({ children }) {
  return (
    <h1 className="pb-2 text-3xl font-extrabold text-green-300 uppercase md:text-5xl">
      {children}
    </h1>
  );
}
