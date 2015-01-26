#pragma once
#include "Common.h"
#include <vector>
#include <sstream>

namespace Profiler
{
	class OutputDataStream : private std::ostringstream 
	{
	public:
		static OutputDataStream Empty;
		// Move constructor rocks!
		// Beware of one copy here(do not use it in performance critical parts)
		std::string GetData();

		// It is important to make private inheritance in order to avoid collision with default operator implementation
		friend OutputDataStream &operator << ( OutputDataStream &stream, const char* val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, int val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, size_t val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, uint32 val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, int64 val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, uint64 val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, char val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, byte val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, const std::string& val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, const std::wstring& val );
	};

	template<class T>
	OutputDataStream& operator<<(OutputDataStream &stream, const std::vector<T>& val)
	{
		stream << val.size();

		for each (const T& element in val)
			stream << element;

		return stream;
	}

	
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class InputDataStream : private std::stringstream {
	public:
		bool CanRead() { return !eof(); }

		InputDataStream( const char *buffer, int length );

		friend InputDataStream &operator >> ( InputDataStream &stream, byte &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, int32 &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, uint32 &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, int64 &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, uint64 &val );
	};


}