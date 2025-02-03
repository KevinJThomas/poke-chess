import Pokemon from "./Pokemon";

export default function OpponentPokemon({ opponent }) {
  return (
    <div className="row">
      {opponent.board.map((pokemon) => (
        <Pokemon key={pokemon.id} {...pokemon} location="opponent" />
      ))}
    </div>
  );
}
