import { CITIES, type City } from "../types/rates";

interface CitySelectorProps {
  selectedCity: City;
  onChange: (city: City) => void;
}

export function CitySelector({ selectedCity, onChange }: CitySelectorProps) {
  return (
    <label className="city-selector">
      City
      <select value={selectedCity} onChange={(e) => onChange(e.target.value as City)}>
        {CITIES.map((city) => (
          <option key={city} value={city}>
            {city}
          </option>
        ))}
      </select>
    </label>
  );
}
